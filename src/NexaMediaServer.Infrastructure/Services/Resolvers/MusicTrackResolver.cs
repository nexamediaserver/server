// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Common;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Infrastructure.Services.Resolvers;

/// <summary>
/// Resolver stub for audio track files in Music libraries.
/// </summary>
/// <remarks>
/// <para>
/// Track resolution is primarily handled by <see cref="MusicAlbumResolver"/> when processing
/// album folders. This resolver exists to catch orphan audio files that are not within
/// an album folder structure, returning null to skip them.
/// </para>
/// <para>
/// The hierarchy is: AlbumReleaseGroup → AlbumRelease → AlbumMedium → Track.
/// Tracks must have a parent AlbumMedium to be properly scanned.
/// </para>
/// </remarks>
public sealed class MusicTrackResolver : ItemResolverBase<Track>
{
    /// <inheritdoc />
    public override string Name => nameof(MusicTrackResolver);

    /// <inheritdoc />
    public override int Priority => 0;

    /// <inheritdoc />
    public override Track? Resolve(ItemResolveArgs args)
    {
        // Only operate within Music libraries
        if (args.LibraryType != LibraryType.Music)
        {
            return null;
        }

        // Only handle files, not directories
        if (args.File.IsDirectory)
        {
            return null;
        }

        // Check if this is an audio file
        if (!MediaFileExtensions.IsAudio(args.File.Extension))
        {
            return null;
        }

        // Track files are handled by MusicAlbumResolver when processing album folders.
        // Individual track files without a parent album structure are not supported.
        // Return null to skip these orphan files.
        return null;
    }
}
