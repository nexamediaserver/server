// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Infrastructure.Services.Resolvers;

/// <summary>
/// Placeholder resolver for artist folders in Music libraries.
/// </summary>
/// <remarks>
/// <para>
/// This resolver detects artist folders but does NOT create Person/Group entities
/// during the scan pipeline. Artist entities should be created by metadata agents
/// when fetching album metadata, which allows for proper deduplication across
/// libraries and linking via MusicBrainz IDs.
/// </para>
/// <para>
/// Artists (Person/Group types) should not have a LibrarySectionId assigned as
/// they are global entities that can appear across multiple music libraries.
/// Album → Artist relationships are established via <see cref="Core.Enums.RelationType"/>
/// values like <c>GroupContributesToAudio</c> or <c>PersonContributesToAudio</c>.
/// </para>
/// <para>
/// This resolver returns null to allow album resolution to proceed for artist
/// folder children. The album resolver handles the album → track hierarchy.
/// </para>
/// </remarks>
public sealed class MusicArtistResolver : ItemResolverBase<MetadataBaseItem>
{
    /// <inheritdoc />
    public override string Name => nameof(MusicArtistResolver);

    /// <inheritdoc />
    public override int Priority => 20; // Higher priority than album resolver for root-level directories

    /// <inheritdoc />
    public override MetadataBaseItem? Resolve(ItemResolveArgs args)
    {
        // Only operate within Music libraries
        if (args.LibraryType != LibraryType.Music)
        {
            return null;
        }

        // Only handle directories
        if (!args.File.IsDirectory)
        {
            return null;
        }

        // Artist folders don't create metadata items during scanning.
        // Artist entities are created by metadata agents when album metadata is fetched.
        // This allows proper deduplication and linking via external IDs (e.g., MusicBrainz).
        // Return null to let the scan continue to process album subfolders.
        return null;
    }
}
