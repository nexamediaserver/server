// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Fields;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Provides field definitions for item detail pages based on metadata type.
/// </summary>
public interface IDetailFieldDefinitionProvider
{
    /// <summary>
    /// Gets the default field definitions for a metadata type.
    /// </summary>
    /// <param name="metadataType">The type of metadata item being viewed.</param>
    /// <returns>A list of field definitions appropriate for the metadata type.</returns>
    IReadOnlyList<DetailFieldDefinition> GetDefaultFields(MetadataType metadataType);

    /// <summary>
    /// Gets the default field group definitions for a metadata type.
    /// </summary>
    /// <param name="metadataType">The type of metadata item being viewed.</param>
    /// <returns>A list of field group definitions appropriate for the metadata type.</returns>
    IReadOnlyList<DetailFieldGroup> GetDefaultGroups(MetadataType metadataType);
}
