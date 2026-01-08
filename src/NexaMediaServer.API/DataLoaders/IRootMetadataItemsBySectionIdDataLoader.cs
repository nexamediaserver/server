// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.API.Types;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.API.DataLoaders;

/// <summary>
/// Abstraction for loading root metadata items grouped by their owning library section UUID.
/// </summary>
public interface IRootMetadataItemsBySectionIdDataLoader
    : IDataLoader<RootMetadataItemsRequest, IReadOnlyList<MetadataItem>>
{ }

/// <summary>
/// Key for loading root metadata items for a given section and type filter.
/// </summary>
/// <param name="SectionId">Owning library section identifier.</param>
/// <param name="MetadataTypes">Metadata type constraints (items matching any of these types are returned).</param>
public readonly record struct RootMetadataItemsRequest(Guid SectionId, MetadataType[] MetadataTypes)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RootMetadataItemsRequest"/> struct
    /// for a single metadata type (backward compatibility).
    /// </summary>
    /// <param name="sectionId">Owning library section identifier.</param>
    /// <param name="metadataType">Single metadata type constraint.</param>
    public RootMetadataItemsRequest(Guid sectionId, MetadataType metadataType)
        : this(sectionId, [metadataType])
    {
    }

    /// <inheritdoc/>
    public bool Equals(RootMetadataItemsRequest other) =>
        this.SectionId == other.SectionId &&
        this.MetadataTypes.SequenceEqual(other.MetadataTypes);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(this.SectionId);
        foreach (var type in this.MetadataTypes)
        {
            hash.Add(type);
        }

        return hash.ToHashCode();
    }
}
