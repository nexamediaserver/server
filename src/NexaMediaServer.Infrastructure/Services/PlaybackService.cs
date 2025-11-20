// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.IO;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.DTOs.Playback;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Default playback orchestration service (skeleton implementation).
/// </summary>
public partial class PlaybackService : IPlaybackService
{
    private readonly IBifService bifService;
    private readonly IDbContextFactory<MediaServerContext> dbContextFactory;
    private readonly IGopIndexService gopIndexService;
    private readonly ILogger<PlaybackService> logger;
    private readonly TranscodeOptions transcodeOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackService"/> class.
    /// </summary>
    /// <param name="dbContextFactory">Factory for creating DbContext instances.</param>
    /// <param name="bifService">BIF service for trickplay discovery.</param>
    /// <param name="gopIndexService">GoP index service for seek optimization.</param>
    /// <param name="logger">Typed logger.</param>
    /// <param name="transcodeOptions">Transcode configuration binding.</param>
    public PlaybackService(
        IDbContextFactory<MediaServerContext> dbContextFactory,
        IBifService bifService,
        IGopIndexService gopIndexService,
        ILogger<PlaybackService> logger,
        IOptions<TranscodeOptions> transcodeOptions
    )
    {
        this.dbContextFactory = dbContextFactory;
        this.bifService = bifService;
        this.gopIndexService = gopIndexService;
        this.logger = logger;
        this.transcodeOptions = transcodeOptions.Value;
    }

    /// <inheritdoc />
    public async Task<CapabilityProfile> UpsertCapabilityProfileAsync(
        int sessionId,
        CapabilityProfileInput input,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var latest = await db
            .CapabilityProfiles.Where(p => p.SessionId == sessionId)
            .OrderByDescending(p => p.Version)
            .FirstOrDefaultAsync(cancellationToken);

        int nextVersion = input.Version ?? (latest?.Version + 1 ?? 1);

        CapabilityProfile profile;
        if (latest != null && latest.Version == nextVersion)
        {
            profile = latest;
        }
        else
        {
            profile = new CapabilityProfile { SessionId = sessionId, Version = nextVersion };
            await db.CapabilityProfiles.AddAsync(profile, cancellationToken);
        }

        profile.DeviceId = input.DeviceId;
        profile.Name = input.Name;
        profile.Capabilities = input.Capabilities ?? new PlaybackCapabilities();
        profile.DeclaredAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return profile;
    }

    /// <inheritdoc />
    public async Task<PlaybackStartResponse> StartPlaybackAsync(
        int sessionId,
        PlaybackStartRequest request,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var profile = await db
            .CapabilityProfiles.Where(p => p.SessionId == sessionId)
            .OrderByDescending(p => p.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (profile == null)
        {
            profile = new CapabilityProfile
            {
                SessionId = sessionId,
                Version = 1,
                Capabilities = new PlaybackCapabilities(),
                DeclaredAt = DateTime.UtcNow,
            };
            await db.CapabilityProfiles.AddAsync(profile, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }

        var metadata = await db.MetadataItems.FirstOrDefaultAsync(
            m => m.Id == request.MetadataItemId,
            cancellationToken
        );

        if (metadata == null)
        {
            throw new InvalidOperationException("Metadata item not found for playback start.");
        }

        var playbackSession = new PlaybackSession
        {
            SessionId = sessionId,
            CapabilityProfile = profile,
            CapabilityProfileId = profile.Id,
            CurrentMetadataItem = metadata,
            CurrentMetadataItemId = metadata.Id,
            Originator = request.Originator,
            LastHeartbeatAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(PlaybackDefaults.ExpiryDays),
            State = "playing",
        };

        PlaybackPlan plan = await this.BuildPlaybackPlanAsync(
            db,
            metadata.Id,
            profile,
            cancellationToken
        );

        playbackSession.CurrentMediaPartId = plan.MediaPartId;

        var generator = new PlaylistGenerator
        {
            PlaybackSession = playbackSession,
            SeedJson = request.ContextJson ?? "{}",
            ExpiresAt = DateTime.UtcNow.AddDays(PlaybackDefaults.ExpiryDays),
            ChunkSize = PlaybackDefaults.PlaylistChunkSize,
        };

        await db.PlaybackSessions.AddAsync(playbackSession, cancellationToken);
        await db.PlaylistGenerators.AddAsync(generator, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return new PlaybackStartResponse
        {
            PlaybackSessionId = playbackSession.PlaybackSessionId,
            PlaylistGeneratorId = generator.PlaylistGeneratorId,
            CapabilityProfileVersion = profile.Version,
            StreamPlanJson = plan.StreamPlanJson,
            PlaybackUrl = plan.PlaybackUrl,
            TrickplayUrl = plan.TrickplayUrl,
            DurationMs = plan.DurationMs,
        };
    }

    /// <inheritdoc />
    public async Task<PlaybackHeartbeatResponse> HeartbeatAsync(
        int sessionId,
        PlaybackHeartbeatRequest request,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var playback = await db
            .PlaybackSessions.Include(p => p.Session)
            .Include(p => p.CapabilityProfile)
            .FirstOrDefaultAsync(
                p => p.PlaybackSessionId == request.PlaybackSessionId && p.SessionId == sessionId,
                cancellationToken
            );

        if (playback == null)
        {
            LogMissingPlaybackSession(this.logger, request.PlaybackSessionId);
            throw new InvalidOperationException(
                $"Playback session {request.PlaybackSessionId} not found."
            );
        }

        playback.PlayheadMs = request.PlayheadMs;
        playback.State = request.State;
        playback.LastHeartbeatAt = DateTime.UtcNow;
        playback.ExpiresAt = DateTime.UtcNow.AddDays(PlaybackDefaults.ExpiryDays);
        if (request.MediaPartId.HasValue)
        {
            playback.CurrentMediaPartId = request.MediaPartId;
        }

        await UpdateProgressAsync(db, playback, request.PlayheadMs, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        // Return the capability version from the already-loaded profile
        return new PlaybackHeartbeatResponse
        {
            PlaybackSessionId = request.PlaybackSessionId,
            CapabilityProfileVersion = playback.CapabilityProfile.Version,
        };
    }

    /// <inheritdoc />
    public async Task<PlaybackDecisionResponse> DecideAsync(
        int sessionId,
        PlaybackDecisionRequest request,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var playback = await db
            .PlaybackSessions.Include(p => p.Session)
            .Include(p => p.CapabilityProfile)
            .Include(p => p.CurrentMetadataItem)
            .FirstOrDefaultAsync(
                p => p.PlaybackSessionId == request.PlaybackSessionId && p.SessionId == sessionId,
                cancellationToken
            );

        if (playback == null)
        {
            throw new InvalidOperationException("Playback session not found for decision.");
        }

        playback.PlayheadMs = request.ProgressMs;
        playback.LastHeartbeatAt = DateTime.UtcNow;
        playback.ExpiresAt = DateTime.UtcNow.AddDays(PlaybackDefaults.ExpiryDays);

        if (string.Equals(request.Status, "ended", StringComparison.OrdinalIgnoreCase))
        {
            await UpdateViewCountAsync(db, playback, cancellationToken);
        }

        // Build stream plan for the current/next item
        PlaybackPlan plan = await this.BuildPlaybackPlanAsync(
            db,
            playback.CurrentMetadataItemId,
            playback.CapabilityProfile,
            cancellationToken
        );

        playback.CurrentMediaPartId = plan.MediaPartId;

        // Persist all changes including CurrentMediaPartId update
        await db.SaveChangesAsync(cancellationToken);

        return new PlaybackDecisionResponse
        {
            Action = "continue",
            StreamPlanJson = plan.StreamPlanJson,
            NextMetadataItemId = playback.CurrentMetadataItemId,
            NextMetadataItemUuid = playback.CurrentMetadataItem?.Uuid,
            PlaybackUrl = plan.PlaybackUrl,
            TrickplayUrl = plan.TrickplayUrl,
            CapabilityProfileVersion = playback.CapabilityProfile.Version,
        };
    }

    /// <inheritdoc />
    public async Task<PlaybackResumeResponse?> ResumeAsync(
        int sessionId,
        Guid playbackSessionId,
        CancellationToken cancellationToken
    )
    {
        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        var playback = await db
            .PlaybackSessions.Include(p => p.Session)
            .Include(p => p.PlaylistGenerator)
            .Include(p => p.CurrentMetadataItem)
            .Include(p => p.CapabilityProfile)
            .FirstOrDefaultAsync(
                p => p.PlaybackSessionId == playbackSessionId && p.SessionId == sessionId,
                cancellationToken
            );

        if (playback == null)
        {
            return null;
        }

        // Return all needed data to avoid redundant queries in the GraphQL layer
        return new PlaybackResumeResponse
        {
            Session = playback,
            CurrentMetadataItemUuid = playback.CurrentMetadataItem?.Uuid ?? Guid.Empty,
            PlaylistGeneratorId = playback.PlaylistGenerator?.PlaylistGeneratorId ?? Guid.Empty,
            CapabilityProfileVersion = playback.CapabilityProfile.Version,
        };
    }

    /// <inheritdoc />
    public async Task<PlaybackSeekResponse> SeekAsync(
        int sessionId,
        PlaybackSeekRequest request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var db = await this.dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Verify the playback session belongs to this session
        var playback = await db
            .PlaybackSessions.Include(p => p.CurrentMetadataItem)
            .FirstOrDefaultAsync(
                p => p.PlaybackSessionId == request.PlaybackSessionId && p.SessionId == sessionId,
                cancellationToken
            );

        if (playback == null)
        {
            throw new InvalidOperationException(
                $"Playback session {request.PlaybackSessionId} not found or does not belong to session {sessionId}."
            );
        }

        // Lookup the media part to get the metadata UUID and part index
        var mediaPart = await db
            .MediaParts.Include(p => p.MediaItem)
                .ThenInclude(mi => mi.MetadataItem)
            .FirstOrDefaultAsync(p => p.Id == request.MediaPartId, cancellationToken);

        if (mediaPart?.MediaItem?.MetadataItem == null)
        {
            // No metadata, return the original target as fallback
            return new PlaybackSeekResponse
            {
                KeyframeMs = request.TargetMs,
                GopDurationMs = 0,
                HasGopIndex = false,
                OriginalTargetMs = request.TargetMs,
            };
        }

        var metadataUuid = mediaPart.MediaItem.MetadataItem.Uuid;

        // Determine part index
        int partIndex = await ResolvePartIndexAsync(db, mediaPart, cancellationToken);

        // Find nearest keyframe using GoP index
        var keyframe = await this.gopIndexService.GetNearestKeyframeAsync(
            metadataUuid,
            partIndex,
            request.TargetMs,
            cancellationToken
        );

        if (keyframe == null)
        {
            // No GoP index available, return the original target
            return new PlaybackSeekResponse
            {
                KeyframeMs = request.TargetMs,
                GopDurationMs = 0,
                HasGopIndex = false,
                OriginalTargetMs = request.TargetMs,
            };
        }

        this.LogSeekToKeyframe(request.PlaybackSessionId, request.TargetMs, keyframe.PtsMs);

        return new PlaybackSeekResponse
        {
            KeyframeMs = keyframe.PtsMs,
            GopDurationMs = keyframe.DurationMs,
            HasGopIndex = true,
            OriginalTargetMs = request.TargetMs,
        };
    }

    private static async Task<int> ResolvePartIndexAsync(
        MediaServerContext db,
        MediaPart part,
        CancellationToken cancellationToken
    )
    {
        var partIds = await db
            .MediaParts.Where(p => p.MediaItemId == part.MediaItemId)
            .OrderBy(p => p.Id)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var index = partIds.FindIndex(id => id == part.Id);
        return index < 0 ? 0 : index;
    }

    private static async Task UpdateProgressAsync(
        MediaServerContext db,
        PlaybackSession playback,
        long playheadMs,
        CancellationToken cancellationToken
    )
    {
        var userId = playback.Session.UserId;
        var metadataId = playback.CurrentMetadataItemId;

        var setting = await db.MetadataItemSettings.FirstOrDefaultAsync(
            s => s.UserId == userId && s.MetadataItemId == metadataId,
            cancellationToken
        );

        if (setting == null)
        {
            setting = new MetadataItemSetting { UserId = userId, MetadataItemId = metadataId };
            await db.MetadataItemSettings.AddAsync(setting, cancellationToken);
        }

        setting.ViewOffset = (int)Math.Min(int.MaxValue, playheadMs);
        setting.LastViewedAt = DateTime.UtcNow;
    }

    private static async Task UpdateViewCountAsync(
        MediaServerContext db,
        PlaybackSession playback,
        CancellationToken cancellationToken
    )
    {
        var userId = playback.Session.UserId;
        var metadataId = playback.CurrentMetadataItemId;

        var setting = await db.MetadataItemSettings.FirstOrDefaultAsync(
            s => s.UserId == userId && s.MetadataItemId == metadataId,
            cancellationToken
        );

        if (setting == null)
        {
            setting = new MetadataItemSetting { UserId = userId, MetadataItemId = metadataId };
            await db.MetadataItemSettings.AddAsync(setting, cancellationToken);
        }

        setting.ViewCount += 1;
        setting.ViewOffset = 0;
        setting.LastViewedAt = DateTime.UtcNow;
    }

    private static bool SupportsDirectPlay(
        PlaybackCapabilities capabilities,
        string container,
        string? videoCodec,
        string? audioCodec
    )
    {
        foreach (var profile in capabilities.DirectPlayProfiles)
        {
            if (!MatchesCsv(container, profile.Container))
            {
                continue;
            }

            bool videoOk = profile.VideoCodec is null || MatchesCsv(videoCodec, profile.VideoCodec);
            bool audioOk = profile.AudioCodec is null || MatchesCsv(audioCodec, profile.AudioCodec);
            if (videoOk && audioOk)
            {
                return true;
            }
        }

        return false;
    }

    private static bool SupportsDirectStream(
        PlaybackCapabilities capabilities,
        string container,
        string? videoCodec,
        string? audioCodec,
        out string remuxContainer,
        out string remuxProtocol
    )
    {
        remuxContainer = container;
        remuxProtocol = capabilities.SupportsDash ? "dash" : "progressive";

        foreach (var profile in capabilities.TranscodingProfiles)
        {
            if (!MatchesCsv(container, profile.Container))
            {
                continue;
            }

            bool videoOk = profile.VideoCodec is null || MatchesCsv(videoCodec, profile.VideoCodec);
            bool audioOk = profile.AudioCodec is null || MatchesCsv(audioCodec, profile.AudioCodec);
            if (videoOk && audioOk)
            {
                remuxContainer = profile.Container ?? remuxContainer;
                remuxProtocol = profile.Protocol ?? remuxProtocol;
                return true;
            }
        }

        return false;
    }

    private static bool MatchesCsv(string? value, string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var tokens = csv.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );
        return tokens.Any(t => string.Equals(t, value, StringComparison.OrdinalIgnoreCase));
    }

    private PlaybackStreamPlan PlanStream(
        string filePath,
        int mediaPartId,
        PlaybackCapabilities? capabilities,
        MediaCodecInfo? codecInfo
    )
    {
        string extension = Path.GetExtension(filePath);
        string normalizedExt = string.IsNullOrWhiteSpace(extension)
            ? string.Empty
            : extension.TrimStart('.').ToLowerInvariant();

        var caps = capabilities ?? new PlaybackCapabilities();

        // Use pre-analyzed codec info from the database when available
        string? videoCodec = codecInfo?.VideoCodec;
        string? audioCodec = codecInfo?.AudioCodec;
        int? videoIndex = codecInfo?.VideoStreamIndex;
        int? audioIndex = codecInfo?.AudioStreamIndex;

        bool directPlay = SupportsDirectPlay(caps, normalizedExt, videoCodec, audioCodec);
        if (directPlay)
        {
            return new PlaybackStreamPlan
            {
                Mode = PlaybackMode.DirectPlay,
                Protocol = "progressive",
                MediaPartId = mediaPartId,
                Container = normalizedExt,
                DirectUrl = $"/api/v1/media/part/{mediaPartId}/file.{normalizedExt}",
                VideoCodec = videoCodec,
                AudioCodec = audioCodec,
                VideoStreamIndex = videoIndex,
                AudioStreamIndex = audioIndex,
                CopyVideo = true,
                CopyAudio = true,
                UseHardwareAcceleration = false,
                EnableToneMapping = false,
            };
        }

        string remuxContainer;
        string remuxProtocol;
        if (
            SupportsDirectStream(
                caps,
                normalizedExt,
                videoCodec,
                audioCodec,
                out remuxContainer,
                out remuxProtocol
            )
        )
        {
            return new PlaybackStreamPlan
            {
                Mode = PlaybackMode.DirectStream,
                Protocol = remuxProtocol,
                MediaPartId = mediaPartId,
                Container = remuxContainer,
                RemuxUrl = $"/api/v1/playback/part/{mediaPartId}/remux.{remuxContainer}",
                VideoCodec = videoCodec,
                AudioCodec = audioCodec,
                VideoStreamIndex = videoIndex,
                AudioStreamIndex = audioIndex,
                CopyVideo = true,
                CopyAudio = true,
                UseHardwareAcceleration =
                    this.transcodeOptions.HardwareAcceleration != HardwareAccelerationKind.None,
                EnableToneMapping = this.ShouldApplyToneMapping(caps),
            };
        }

        bool useHwAccel =
            this.transcodeOptions.HardwareAcceleration != HardwareAccelerationKind.None;

        return new PlaybackStreamPlan
        {
            Mode = PlaybackMode.Transcode,
            Protocol = "dash",
            MediaPartId = mediaPartId,
            Container = "mp4",
            ManifestUrl = $"/api/v1/playback/part/{mediaPartId}/dash/manifest.mpd",
            VideoCodec = this.transcodeOptions.DashVideoCodec,
            AudioCodec = this.transcodeOptions.DashAudioCodec,
            VideoStreamIndex = videoIndex,
            AudioStreamIndex = audioIndex,
            CopyVideo = false,
            CopyAudio = false,
            UseHardwareAcceleration = useHwAccel,
            EnableToneMapping = this.ShouldApplyToneMapping(caps),
        };
    }

    private bool ShouldApplyToneMapping(PlaybackCapabilities capabilities)
    {
        if (!this.transcodeOptions.EnableToneMapping)
        {
            return false;
        }

        if (!capabilities.AllowToneMapping)
        {
            return false;
        }

        return !capabilities.SupportsHdr;
    }

    private async Task<PlaybackPlan> BuildPlaybackPlanAsync(
        MediaServerContext db,
        int metadataItemId,
        CapabilityProfile? capabilityProfile,
        CancellationToken cancellationToken
    )
    {
        // Optimized query: fetch media part + codec info in a single round-trip
        // Include both MediaItem.Duration (from file analysis) and MetadataItem.Duration (from external metadata)
        var projection = await db
            .MediaParts.Where(p => p.MediaItem.MetadataItemId == metadataItemId)
            .OrderBy(p => p.Id)
            .Select(p => new
            {
                p.Id,
                p.File,
                MetadataUuid = p.MediaItem.MetadataItem.Uuid,
                p.MediaItem.VideoCodec,
                AudioCodec = p.MediaItem.AudioCodecs.FirstOrDefault(),
                MediaItemDuration = p.MediaItem.Duration,
                MetadataItemDurationSeconds = p.MediaItem.MetadataItem.Duration,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (projection == null)
        {
            throw new InvalidOperationException(
                $"No media parts available for metadata item {metadataItemId}."
            );
        }

        string? trickplayUrl = null;
        var bifPath = this.bifService.GetBifPath(projection.MetadataUuid, partIndex: 0);
        if (File.Exists(bifPath))
        {
            trickplayUrl = $"/api/v1/images/trickplay/{projection.Id}.vtt";
        }

        // Use cached codec info from the database instead of calling FFProbe
        var codecInfo = new MediaCodecInfo(
            projection.VideoCodec,
            projection.AudioCodec,
            VideoStreamIndex: 0,
            AudioStreamIndex: 1
        );

        var plan = this.PlanStream(
            projection.File,
            projection.Id,
            capabilityProfile?.Capabilities,
            codecInfo
        );

        string playbackUrl = plan.DirectUrl ?? plan.RemuxUrl ?? plan.ManifestUrl ?? string.Empty;
        string streamPlanJson = JsonSerializer.Serialize(plan);

        // Convert duration to milliseconds, preferring MediaItem.Duration (from file analysis)
        // and falling back to MetadataItem.Duration (from external metadata, in seconds)
        long? durationMs = null;
        if (projection.MediaItemDuration.HasValue)
        {
            durationMs = (long)projection.MediaItemDuration.Value.TotalMilliseconds;
        }
        else if (projection.MetadataItemDurationSeconds.HasValue)
        {
            durationMs = projection.MetadataItemDurationSeconds.Value * 1000L;
        }

        return new PlaybackPlan(
            projection.Id,
            playbackUrl,
            streamPlanJson,
            trickplayUrl,
            durationMs
        );
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Seek in session {PlaybackSessionId}: target={TargetMs}ms -> keyframe={KeyframeMs}ms"
    )]
    private partial void LogSeekToKeyframe(Guid playbackSessionId, long targetMs, long keyframeMs);

    /// <summary>
    /// Holds pre-analyzed codec information from the database.
    /// </summary>
    /// <param name="VideoCodec">The video codec.</param>
    /// <param name="AudioCodec">The audio codec.</param>
    /// <param name="VideoStreamIndex">The video stream index.</param>
    /// <param name="AudioStreamIndex">The audio stream index.</param>
    private sealed record MediaCodecInfo(
        string? VideoCodec,
        string? AudioCodec,
        int? VideoStreamIndex,
        int? AudioStreamIndex
    );

    private sealed record PlaybackPlan(
        int MediaPartId,
        string PlaybackUrl,
        string StreamPlanJson,
        string? TrickplayUrl,
        long? DurationMs
    );
}
