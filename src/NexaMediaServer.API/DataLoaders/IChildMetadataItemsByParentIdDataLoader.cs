// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.API.Types;

namespace NexaMediaServer.API.DataLoaders;

/// <summary>
/// Abstraction for loading child metadata items grouped by their parent item UUID.
/// </summary>
public interface IChildMetadataItemsByParentIdDataLoader
    : IDataLoader<Guid, IReadOnlyList<MetadataItem>>
{ }
