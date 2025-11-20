// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using FFMpegCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexaMediaServer.Common;
using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Services;
using IODirectory = System.IO.Directory;

namespace NexaMediaServer.Infrastructure.Services.Images;

/// <summary>
/// Image provider that generates BIF (Base Index Frames) trickplay files for video scrubbing.
/// Extracts video snapshots using batch FFmpeg processing with GoP index alignment for optimal i-frame selection.
/// </summary>
public partial class TrickplayImageProvider : IImageProvider<Video>
{
    /// <summary>
    /// Maximum degree of parallelism for reading frame files from disk.
    /// </summary>
    private const int MaxParallelFileReads = 8;

    private readonly ILogger<TrickplayImageProvider> logger;
    private readonly IGopIndexService gopIndexService;
    private readonly IBifService bifService;
    private readonly IApplicationPaths paths;
    private readonly TrickplayOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrickplayImageProvider"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="gopIndexService">The GoP index service.</param>
    /// <param name="bifService">The BIF service.</param>
    /// <param name="paths">Application paths service.</param>
    /// <param name="options">Trickplay configuration options.</param>
    public TrickplayImageProvider(
        ILogger<TrickplayImageProvider> logger,
        IGopIndexService gopIndexService,
        IBifService bifService,
        IApplicationPaths paths,
        IOptions<TrickplayOptions> options
    )
    {
        this.logger = logger;
        this.gopIndexService = gopIndexService;
        this.bifService = bifService;
        this.paths = paths;
        this.options = options.Value;
    }

    /// <inheritdoc />
    public string Name => "Trickplay BIF Generator";

    /// <inheritdoc />
    public int Order => 1000; // Run after everything else

    /// <inheritdoc />
    public Task ProvideAsync(
        MediaItem item,
        Video metadata,
        IReadOnlyList<MediaPart> parts,
        CancellationToken cancellationToken
    ) => this.ProvideInternalAsync(item, metadata, parts, cancellationToken);

    /// <inheritdoc />
    public bool Supports(MediaItem item, Video metadata) => this.SupportsMediaItem(item);

    /// <summary>
    /// Builds an FFmpeg filter expression for extracting frames at specific intervals.
    /// </summary>
    private static string BuildFilterExpression(int intervalMs, int maxWidth)
    {
        // Use fps filter for reliable time-based frame extraction
        // fps filter automatically selects frames at the specified rate
        var fps = 1000.0 / intervalMs; // Convert interval to frames per second
        return $"fps={fps:F6},scale={maxWidth}:-1";
    }

    private async Task ProvideInternalAsync(
        MediaItem item,
        MetadataBaseItem metadata,
        IReadOnlyList<MediaPart> parts,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(metadata);
        cancellationToken.ThrowIfCancellationRequested();
        this.LogProvideStart(item.Id, metadata.Uuid, parts?.Count ?? 0);

        // Basic guards
        if (parts == null || parts.Count == 0)
        {
            this.LogNoPartsForTrickplay(item.Id);
            this.LogProvideSkipped(item.Id, "no parts");
            return;
        }

        var firstPart = parts[0];
        if (string.IsNullOrWhiteSpace(firstPart.File) || !File.Exists(firstPart.File))
        {
            this.LogMissingPartFile(item.Id, firstPart.File ?? "<null>");
            this.LogProvideSkipped(item.Id, "missing part file");
            return;
        }

        // Only process video files
        var ext = Path.GetExtension(firstPart.File);
        if (!MediaFileExtensions.IsVideo(ext))
        {
            this.LogSkipNonVideo(item.Id, ext);
            this.LogProvideSkipped(item.Id, "not video");
            return;
        }

        await this.GenerateTrickplayBifAsync(item, metadata, firstPart, 0, cancellationToken)
            .ConfigureAwait(false);
        this.LogProvideComplete(item.Id);
    }

    private bool SupportsMediaItem(MediaItem item)
    {
        if (item == null)
        {
            return false;
        }

        var first = item.Parts?.FirstOrDefault();
        if (first == null)
        {
            this.LogNotSupportedNoParts(item.Id);
            return false;
        }

        if (string.IsNullOrWhiteSpace(first.File))
        {
            this.LogNotSupportedBlankPath(item.Id);
            return false;
        }

        var ext = Path.GetExtension(first.File);
        var isVideo = MediaFileExtensions.IsVideo(ext);
        this.LogSupportEvaluation(item.Id, ext, isVideo);
        return isVideo;
    }

    /// <summary>
    /// Attempts to extract snapshots using a single batch FFmpeg command.
    /// This is the most efficient method, extracting all frames in one pass.
    /// </summary>
    private async Task<BifFile?> TryBatchExtractSnapshotsAsync(
        MediaItem item,
        MetadataBaseItem metadata,
        MediaPart part,
        List<long> targetTimestamps,
        CancellationToken cancellationToken
    )
    {
        var tempDir = Path.Combine(this.paths.TempDirectory, $"trickplay_{metadata.Uuid:N}");

        try
        {
            // Create temporary directory
            IODirectory.CreateDirectory(tempDir);
            this.LogBatchExtractionStart(item.Id, targetTimestamps.Count, tempDir);

            // Build FFmpeg filter expression for frame extraction and scaling
            var filterExpression = BuildFilterExpression(
                this.options.SnapshotIntervalMs,
                this.options.MaxSnapshotWidth
            );

            // Build output pattern
            var outputPattern = Path.Combine(tempDir, "frame_%04d.jpg");

            // Execute batch extraction
            var success = await this.ExecuteBatchExtractionAsync(
                    part.File,
                    outputPattern,
                    filterExpression,
                    this.options.JpegQuality,
                    cancellationToken
                )
                .ConfigureAwait(false);

            if (!success)
            {
                this.LogBatchExtractionFailed(item.Id);
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Enumerate files without creating intermediate list - sort using natural string comparison
            var frameFiles = IODirectory
                .EnumerateFiles(tempDir, "frame_*.jpg")
                .Order(StringComparer.Ordinal)
                .ToArray();

            var frameCount = frameFiles.Length;
            var entriesToProcess = Math.Min(frameCount, targetTimestamps.Count);

            this.LogBatchExtractionComplete(item.Id, frameCount);

            if (entriesToProcess == 0)
            {
                return null;
            }

            // Pre-allocate BIF file with known capacity
            var bifFile = new BifFile { Version = 0 };
            bifFile.Entries.EnsureCapacity(entriesToProcess);
            bifFile.ImageData.EnsureCapacity(entriesToProcess);

            // Read frame files in parallel with bounded concurrency
            var frameDataArray = new (int Index, int TimestampKey, byte[]? Data)[entriesToProcess];

            await Parallel
                .ForEachAsync(
                    Enumerable.Range(0, entriesToProcess),
                    new ParallelOptions
                    {
                        MaxDegreeOfParallelism = MaxParallelFileReads,
                        CancellationToken = cancellationToken,
                    },
                    async (i, ct) =>
                    {
                        var timestampKey = (int)targetTimestamps[i];
                        try
                        {
                            var imageBytes = await File.ReadAllBytesAsync(frameFiles[i], ct)
                                .ConfigureAwait(false);
                            frameDataArray[i] = (i, timestampKey, imageBytes);
                        }
                        catch (Exception ex)
                        {
                            this.LogSnapshotException(ex, item.Id, targetTimestamps[i]);
                            frameDataArray[i] = (i, timestampKey, null);
                        }
                    }
                )
                .ConfigureAwait(false);

            // Build BIF file sequentially (entries must be in order)
            var successCount = 0;
            for (int i = 0; i < entriesToProcess; i++)
            {
                var (_, timestampKey, data) = frameDataArray[i];
                if (data != null)
                {
                    bifFile.Entries.Add(new BifEntry(timestampKey, 0));
                    bifFile.ImageData[timestampKey] = data;
                    successCount++;
                }

                if ((i + 1) % 100 == 0)
                {
                    this.LogBatchProgress(item.Id, i + 1, entriesToProcess);
                }
            }

            this.LogParallelReadComplete(item.Id, successCount, entriesToProcess);
            return bifFile;
        }
        finally
        {
            // Clean up temporary directory
            try
            {
                if (IODirectory.Exists(tempDir))
                {
                    IODirectory.Delete(tempDir, true);
                }
            }
            catch (Exception ex)
            {
                this.LogTempCleanupFailed(tempDir, ex);
            }
        }
    }

    /// <summary>
    /// Executes the batch FFmpeg extraction command.
    /// </summary>
    private async Task<bool> ExecuteBatchExtractionAsync(
        string inputFile,
        string outputPattern,
        string filterExpression,
        int quality,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Calculate q:v value (2=best quality, 31=worst quality for MJPEG)
            var qValue = Math.Max(2, Math.Min(31, 31 - ((quality * 29) / 100)));

            // Build FFmpeg arguments manually for better control
            // ffmpeg -i input.mp4 -vf "fps=0.5,scale=320:-1" -vsync 0 -q:v 3 output_%04d.jpg
            var arguments =
                $"-i \"{inputFile}\" "
                + $"-vf \"{filterExpression}\" "
                + $"-vsync 0 "
                + $"-q:v {qValue} "
                + $"-f image2 "
                + $"\"{outputPattern}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = GlobalFFOptions.GetFFMpegBinaryPath(),
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var process = new Process { StartInfo = startInfo };
            var errorBuilder = new System.Text.StringBuilder();

            process.OutputDataReceived += (sender, e) => {
                // Discard output to avoid memory buildup
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                this.LogBatchFFmpegError(process.ExitCode, errorBuilder.ToString());
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            this.LogBatchFFmpegException(ex);
            return false;
        }
    }

    /// <summary>
    /// Generates a BIF trickplay file for the given video part.
    /// </summary>
    private async Task GenerateTrickplayBifAsync(
        MediaItem item,
        MetadataBaseItem metadata,
        MediaPart part,
        int partIndex,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Early exit: Check if BIF file already exists
        if (this.options.SkipExisting)
        {
            var bifPath = this.bifService.GetBifPath(metadata.Uuid, partIndex);
            if (File.Exists(bifPath))
            {
                this.LogBifAlreadyExists(item.Id, bifPath);
                return;
            }
        }

        // Load GoP index
        var gop = await this
            .gopIndexService.TryReadForPartAsync(item, part, cancellationToken)
            .ConfigureAwait(false);

        if (gop == null || gop.Groups.Count == 0)
        {
            this.LogNoGopIndex(item.Id);
            return;
        }

        // Determine total duration
        var lastGroup = gop.Groups[^1];
        var totalDurationMs = lastGroup.PtsMs + Math.Max(0, lastGroup.DurationMs);
        this.LogGenerateTrickplayStart(item.Id, part.File, gop.Groups.Count, totalDurationMs);

        // Calculate target timestamps
        var targetTimestamps = new List<long>();
        for (long ts = 0; ts < totalDurationMs; ts += this.options.SnapshotIntervalMs)
        {
            targetTimestamps.Add(ts);
        }

        if (targetTimestamps.Count == 0)
        {
            this.LogNoSnapshotsGenerated(item.Id);
            return;
        }

        // Try batch extraction (most efficient)
        var bifFile = await this.TryBatchExtractSnapshotsAsync(
                item,
                metadata,
                part,
                targetTimestamps,
                cancellationToken
            )
            .ConfigureAwait(false);

        // Write BIF file if we have any snapshots
        if (bifFile != null && bifFile.Entries.Count > 0)
        {
            await this
                .bifService.WriteAsync(metadata.Uuid, partIndex, bifFile, cancellationToken)
                .ConfigureAwait(false);
            var bifPath = this.bifService.GetBifPath(metadata.Uuid, partIndex);
            this.LogTrickplaySuccess(item.Id, bifFile.Entries.Count, bifPath);
        }
        else
        {
            this.LogNoSnapshotsGenerated(item.Id);
        }
    }

    #region Logging
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "BIF file already exists, skipping: mediaItem={MediaItemId} path={BifPath}"
    )]
    private partial void LogBifAlreadyExists(int MediaItemId, string BifPath);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Starting batch extraction: mediaItem={MediaItemId} snapshots={SnapshotCount} tempDir={TempDir}"
    )]
    private partial void LogBatchExtractionStart(
        int MediaItemId,
        int SnapshotCount,
        string TempDir
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Batch extraction failed for mediaItem={MediaItemId}, falling back to sequential extraction"
    )]
    private partial void LogBatchExtractionFailed(int MediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Batch extraction complete: mediaItem={MediaItemId} frames={FrameCount}"
    )]
    private partial void LogBatchExtractionComplete(int MediaItemId, int FrameCount);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Batch processing progress: mediaItem={MediaItemId} processed={Processed}/{Total}"
    )]
    private partial void LogBatchProgress(int MediaItemId, int Processed, int Total);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Parallel read complete: mediaItem={MediaItemId} success={SuccessCount}/{TotalCount}"
    )]
    private partial void LogParallelReadComplete(int MediaItemId, int SuccessCount, int TotalCount);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to clean up temp directory: {TempDir}"
    )]
    private partial void LogTempCleanupFailed(string TempDir, Exception ex);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "FFmpeg batch extraction failed with exit code {ExitCode}: {Error}"
    )]
    private partial void LogBatchFFmpegError(int ExitCode, string Error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "FFmpeg batch extraction threw exception")]
    private partial void LogBatchFFmpegException(Exception ex);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Trickplay provider start: mediaItem={MediaItemId} metadata={MetadataUuid} parts={PartCount}"
    )]
    private partial void LogProvideStart(int MediaItemId, Guid MetadataUuid, int PartCount);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Trickplay provider skipped: mediaItem={MediaItemId} reason={Reason}"
    )]
    private partial void LogProvideSkipped(int MediaItemId, string Reason);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Trickplay provider complete: mediaItem={MediaItemId}"
    )]
    private partial void LogProvideComplete(int MediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Trickplay not supported: no parts for media item {MediaItemId}"
    )]
    private partial void LogNotSupportedNoParts(int mediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Trickplay not supported: blank part path for media item {MediaItemId}"
    )]
    private partial void LogNotSupportedBlankPath(int mediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Trickplay support eval media item {MediaItemId}: ext={Extension} video={IsVideo}"
    )]
    private partial void LogSupportEvaluation(int mediaItemId, string? extension, bool isVideo);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Trickplay skip: media item {MediaItemId} has no parts."
    )]
    private partial void LogNoPartsForTrickplay(int mediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Trickplay skip: part file missing for media item {MediaItemId} path={Path}"
    )]
    private partial void LogMissingPartFile(int mediaItemId, string Path);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Trickplay skip: non-video media item {MediaItemId} ext={Extension}"
    )]
    private partial void LogSkipNonVideo(int mediaItemId, string? Extension);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No GoP index available for media item {MediaItemId}, skipping trickplay generation"
    )]
    private partial void LogNoGopIndex(int mediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "GenerateTrickplay start: mediaItem={MediaItemId} path={Path} gopGroups={GopGroups} durationMs={DurationMs}"
    )]
    private partial void LogGenerateTrickplayStart(
        int MediaItemId,
        string Path,
        int GopGroups,
        long DurationMs
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Generated trickplay snapshot: item={MediaItemId} targetMs={TargetMs} actualMs={ActualMs} path={TempPath}"
    )]
    private partial void LogSnapshotGenerated(
        int MediaItemId,
        long TargetMs,
        long ActualMs,
        string TempPath
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to generate trickplay snapshot for item={MediaItemId} at {Ms}ms"
    )]
    private partial void LogSnapshotFailed(int MediaItemId, long Ms);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Exception generating trickplay snapshot for item={MediaItemId} at {Ms}ms"
    )]
    private partial void LogSnapshotException(Exception ex, int MediaItemId, long Ms);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No snapshots generated for media item {MediaItemId}"
    )]
    private partial void LogNoSnapshotsGenerated(int mediaItemId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Trickplay success: mediaItem={MediaItemId} frames={FrameCount} bifPath={BifPath}"
    )]
    private partial void LogTrickplaySuccess(int MediaItemId, int FrameCount, string BifPath);
    #endregion
}
