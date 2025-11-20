// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.API.Types;

namespace NexaMediaServer.API.DataLoaders;

/// <summary>
/// Abstraction for loading extras associated with a metadata item.
/// </summary>
public interface IExtrasByMetadataIdDataLoader : IDataLoader<int, IReadOnlyList<MetadataItem>> { }
