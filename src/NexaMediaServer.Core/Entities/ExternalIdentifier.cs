// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Entities;

/// <summary>
/// Represents an external identifier from a metadata provider (e.g., TMDB, TVDB, MusicBrainz).
/// </summary>
/// <remarks>
/// External identifiers are used to link metadata items to their corresponding entries
/// in external metadata sources. Each metadata item can have multiple external identifiers
/// from different providers, but only one identifier per provider.
/// </remarks>
public class ExternalIdentifier : AuditableEntity
{
    /// <summary>
    /// Gets or sets the identifier of the associated metadata item.
    /// </summary>
    public int MetadataItemId { get; set; }

    /// <summary>
    /// Gets or sets the associated metadata item.
    /// </summary>
    public MetadataItem MetadataItem { get; set; } = null!;

    /// <summary>
    /// Gets or sets the provider name that issued this identifier.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provider names should follow a consistent naming convention. For providers with
    /// multiple identifier types, use namespaced keys (e.g., "musicbrainz_track",
    /// "musicbrainz_release", "musicbrainz_artist").
    /// </para>
    /// <para>
    /// Common providers include:
    /// <list type="bullet">
    ///   <item><description>tmdb - The Movie Database</description></item>
    ///   <item><description>tvdb - TheTVDB</description></item>
    ///   <item><description>imdb - IMDb</description></item>
    ///   <item><description>musicbrainz_track - MusicBrainz track ID</description></item>
    ///   <item><description>musicbrainz_recording - MusicBrainz recording ID</description></item>
    ///   <item><description>musicbrainz_release - MusicBrainz release ID</description></item>
    ///   <item><description>musicbrainz_release_group - MusicBrainz release group ID</description></item>
    ///   <item><description>musicbrainz_artist - MusicBrainz artist ID</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public string Provider { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier value from the provider.
    /// </summary>
    /// <remarks>
    /// The format of the value depends on the provider. For example:
    /// <list type="bullet">
    ///   <item><description>TMDB: numeric ID (e.g., "550")</description></item>
    ///   <item><description>IMDb: string ID (e.g., "tt0137523")</description></item>
    ///   <item><description>MusicBrainz: UUID (e.g., "f27ec8db-af05-4f36-916e-3d57f91ecf5e")</description></item>
    /// </list>
    /// </remarks>
    public string Value { get; set; } = null!;
}
