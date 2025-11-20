// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.API.Types;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.API.DataLoaders;

/// <summary>
/// Abstraction for loading root metadata items grouped by their owning library section UUID.
/// </summary>
public interface IRootMetadataItemsBySectionIdDataLoader
    : IDataLoader<RootMetadataItemsRequest, IReadOnlyList<MetadataItem>> { }

/// <summary>
/// Key for loading root metadata items for a given section and type filter.
/// </summary>
/// <param name="SectionId">Owning library section identifier.</param>
/// <param name="MetadataType">Metadata type constraint.</param>
public readonly record struct RootMetadataItemsRequest(Guid SectionId, MetadataType MetadataType);
