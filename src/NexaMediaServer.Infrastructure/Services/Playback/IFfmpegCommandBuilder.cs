// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.IO;
using System.Threading;
using System.Threading.Tasks;

using NexaMediaServer.Core.Configuration;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Builds and executes FFmpeg commands used for playback delivery.
/// </summary>
public interface IFfmpegCommandBuilder
{
    /// <summary>
    /// Remuxes a source file to the target container and streams it to the provided output.
    /// </summary>
    /// <param name="inputPath">Full path to the source media.</param>
    /// <param name="targetContainer">Target container (e.g., mp4).</param>
    /// <param name="output">Stream to write to.</param>
    /// <param name="hardwareAcceleration">Hardware acceleration preference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the remux operation.</returns>
    Task RemuxToStreamAsync(
        string inputPath,
        string targetContainer,
        Stream output,
        HardwareAccelerationKind hardwareAcceleration,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Remuxes a source file to the target container starting from a specific timestamp.
    /// Seeking to a keyframe boundary ensures fast and accurate seeking.
    /// </summary>
    /// <param name="inputPath">Full path to the source media.</param>
    /// <param name="targetContainer">Target container (e.g., mp4).</param>
    /// <param name="output">Stream to write to.</param>
    /// <param name="seekMs">The position in milliseconds to seek to before starting the remux.</param>
    /// <param name="hardwareAcceleration">Hardware acceleration preference.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the remux operation.</returns>
    Task RemuxToStreamWithSeekAsync(
        string inputPath,
        string targetContainer,
        Stream output,
        long seekMs,
        HardwareAccelerationKind hardwareAcceleration,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Creates a DASH manifest and segments on disk according to the job parameters.
    /// </summary>
    /// <param name="job">Dash job description.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the transcode operation.</returns>
    Task CreateDashAsync(DashTranscodeJob job, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a DASH manifest and segments on disk starting from a specific timestamp.
    /// </summary>
    /// <param name="job">Dash job description.</param>
    /// <param name="seekMs">The position in milliseconds to seek to before starting the transcode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the transcode operation.</returns>
    Task CreateDashWithSeekAsync(
        DashTranscodeJob job,
        long seekMs,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Creates an HLS playlist and segments on disk according to the job parameters.
    /// </summary>
    /// <param name="job">HLS job description.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the transcode operation.</returns>
    Task CreateHlsAsync(HlsTranscodeJob job, CancellationToken cancellationToken);

    /// <summary>
    /// Creates an HLS playlist and segments on disk starting from a specific timestamp.
    /// </summary>
    /// <param name="job">HLS job description.</param>
    /// <param name="seekMs">The position in milliseconds to seek to before starting the transcode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the transcode operation.</returns>
    Task CreateHlsWithSeekAsync(
        HlsTranscodeJob job,
        long seekMs,
        CancellationToken cancellationToken
    );
}
