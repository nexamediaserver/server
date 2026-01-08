// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Fields;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Service for retrieving and managing field definitions for item detail pages.
/// </summary>
public interface IDetailFieldService
{
    /// <summary>
    /// Gets field definitions for a metadata item's detail page.
    /// </summary>
    /// <param name="metadataItemId">The UUID of the metadata item.</param>
    /// <param name="userId">The ID of the user (for applying user preferences).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of field definitions for the item detail page.</returns>
    Task<IReadOnlyList<DetailFieldDefinition>> GetItemDetailFieldDefinitionsAsync(
        Guid metadataItemId,
        string userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets field definitions for a specific metadata type.
    /// </summary>
    /// <param name="metadataType">The metadata type.</param>
    /// <param name="userId">The ID of the user (for applying user preferences).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of field definitions for the metadata type.</returns>
    Task<IReadOnlyList<DetailFieldDefinition>> GetFieldDefinitionsForTypeAsync(
        MetadataType metadataType,
        string userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets the admin-defined field configuration for a metadata type and optional library.
    /// </summary>
    /// <param name="metadataType">The metadata type.</param>
    /// <param name="librarySectionId">Optional library section UUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stored configuration if present; otherwise null.</returns>
    Task<DetailFieldConfiguration?> GetAdminFieldConfigurationAsync(
        MetadataType metadataType,
        Guid? librarySectionId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Updates the field visibility configuration for a user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="metadataType">The metadata type this configuration applies to.</param>
    /// <param name="configuration">The new field configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated configuration.</returns>
    Task<DetailFieldConfiguration> UpdateUserFieldConfigurationAsync(
        string userId,
        MetadataType metadataType,
        DetailFieldConfiguration configuration,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Updates the admin-defined field configuration for a metadata type and optional library.
    /// </summary>
    /// <param name="metadataType">The metadata type.</param>
    /// <param name="librarySectionId">Optional library section UUID to scope the configuration.</param>
    /// <param name="configuration">The new configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted configuration.</returns>
    Task<DetailFieldConfiguration> UpdateAdminFieldConfigurationAsync(
        MetadataType metadataType,
        Guid? librarySectionId,
        DetailFieldConfiguration configuration,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets all custom field definitions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all custom field definitions.</returns>
    Task<IReadOnlyList<CustomFieldDefinition>> GetCustomFieldDefinitionsAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets custom field definitions applicable to a specific metadata type.
    /// </summary>
    /// <param name="metadataType">The metadata type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of custom field definitions applicable to the metadata type.</returns>
    Task<IReadOnlyList<CustomFieldDefinition>> GetCustomFieldDefinitionsForTypeAsync(
        MetadataType metadataType,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Creates a new custom field definition.
    /// </summary>
    /// <param name="key">The unique key for the field.</param>
    /// <param name="label">The display label.</param>
    /// <param name="widget">The widget type.</param>
    /// <param name="applicableMetadataTypes">The metadata types this field applies to.</param>
    /// <param name="sortOrder">The display order.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created custom field definition.</returns>
    Task<CustomFieldDefinition> CreateCustomFieldDefinitionAsync(
        string key,
        string label,
        DetailFieldWidgetType widget,
        IEnumerable<MetadataType> applicableMetadataTypes,
        int sortOrder,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Updates an existing custom field definition.
    /// </summary>
    /// <param name="id">The ID of the custom field definition.</param>
    /// <param name="label">The new display label.</param>
    /// <param name="widget">The new widget type.</param>
    /// <param name="applicableMetadataTypes">The new applicable metadata types.</param>
    /// <param name="sortOrder">The new display order.</param>
    /// <param name="isEnabled">Whether the field is enabled.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated custom field definition.</returns>
    Task<CustomFieldDefinition> UpdateCustomFieldDefinitionAsync(
        int id,
        string? label,
        DetailFieldWidgetType? widget,
        IEnumerable<MetadataType>? applicableMetadataTypes,
        int? sortOrder,
        bool? isEnabled,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Deletes a custom field definition.
    /// </summary>
    /// <param name="id">The ID of the custom field definition to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the field was deleted, false if it was not found.</returns>
    Task<bool> DeleteCustomFieldDefinitionAsync(
        int id,
        CancellationToken cancellationToken = default
    );
}
