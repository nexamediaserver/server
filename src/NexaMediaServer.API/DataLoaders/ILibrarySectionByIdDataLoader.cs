// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.API.Types;

namespace NexaMediaServer.API.DataLoaders;

/// <summary>
/// Abstraction for loading <see cref="LibrarySection"/> instances by their UUID identifiers.
/// </summary>
public interface ILibrarySectionByIdDataLoader : IDataLoader<Guid, LibrarySection> { }
