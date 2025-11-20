// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.API.DataLoaders;

/// <summary>
/// Abstraction for loading per-user metadata item settings keyed by metadata item identifier.
/// </summary>
public interface IMetadataItemSettingByUserDataLoader : IDataLoader<int, MetadataItemSetting?> { }
