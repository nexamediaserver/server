// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Hubs;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Provides hub definitions for different contexts and library/metadata types.
/// </summary>
public interface IHubDefinitionProvider
{
    /// <summary>
    /// Gets the default hub definitions for a library type in the specified context.
    /// </summary>
    /// <param name="libraryType">The type of library.</param>
    /// <param name="context">The hub context (Home or LibraryDiscover).</param>
    /// <returns>A list of hub definitions appropriate for the library type.</returns>
    IReadOnlyList<HubDefinition> GetDefaultHubs(LibraryType libraryType, HubContext context);

    /// <summary>
    /// Gets the default hub definitions for an item detail page based on metadata type.
    /// </summary>
    /// <param name="metadataType">The type of metadata item being viewed.</param>
    /// <param name="childCount">Optional child count for conditional hub logic (e.g., album releases).</param>
    /// <returns>A list of hub definitions appropriate for the item detail page.</returns>
    IReadOnlyList<HubDefinition> GetItemDetailHubs(MetadataType metadataType, int? childCount = null);
}
