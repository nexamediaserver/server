// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Claims;

using HotChocolate.Authorization;

using NexaMediaServer.API.Types.Fields;
using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Defines GraphQL query operations for detail fields.
/// </summary>
public static partial class Query
{
    /// <summary>
    /// Gets field definitions for a metadata item's detail page.
    /// </summary>
    /// <param name="itemId">The metadata item ID.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor for getting the current user.</param>
    /// <param name="fieldService">The field service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of field definitions.</returns>
    [Authorize]
    public static async Task<IEnumerable<DetailFieldDefinitionType>> GetItemDetailFieldDefinitionsAsync(
        [ID] Guid itemId,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] IDetailFieldService fieldService,
        CancellationToken cancellationToken
    )
    {
        var userId = GetUserId(httpContextAccessor);
        var definitions = await fieldService.GetItemDetailFieldDefinitionsAsync(
            itemId,
            userId,
            cancellationToken
        );
        return definitions.Select(MapToDetailFieldDefinitionType);
    }

    /// <summary>
    /// Gets field definitions for a specific metadata type.
    /// </summary>
    /// <param name="metadataType">The metadata type.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor for getting the current user.</param>
    /// <param name="fieldService">The field service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of field definitions.</returns>
    [Authorize]
    public static async Task<IEnumerable<DetailFieldDefinitionType>> GetFieldDefinitionsForTypeAsync(
        MetadataType metadataType,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] IDetailFieldService fieldService,
        CancellationToken cancellationToken
    )
    {
        var userId = GetUserId(httpContextAccessor);
        var definitions = await fieldService.GetFieldDefinitionsForTypeAsync(
            metadataType,
            userId,
            cancellationToken
        );
        return definitions.Select(MapToDetailFieldDefinitionType);
    }

    /// <summary>
    /// Gets all custom field definitions (admin only).
    /// </summary>
    /// <param name="fieldService">The field service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of custom field definitions.</returns>
    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    public static async Task<IEnumerable<CustomFieldDefinitionType>> GetCustomFieldDefinitionsAsync(
        [Service] IDetailFieldService fieldService,
        CancellationToken cancellationToken
    )
    {
        var customFields = await fieldService.GetCustomFieldDefinitionsAsync(cancellationToken);
        return customFields.Select(MapToCustomFieldDefinitionType);
    }

    private static DetailFieldDefinitionType MapToDetailFieldDefinitionType(
        Core.DTOs.Fields.DetailFieldDefinition definition
    )
    {
        return new DetailFieldDefinitionType
        {
            Key = definition.FieldType == DetailFieldType.Custom
                ? definition.CustomFieldKey ?? string.Empty
                : definition.FieldType.ToString(),
            FieldType = definition.FieldType,
            Label = definition.Label,
            Widget = definition.Widget,
            SortOrder = definition.SortOrder,
            CustomFieldKey = definition.CustomFieldKey,
            GroupKey = definition.GroupKey,
        };
    }

    private static CustomFieldDefinitionType MapToCustomFieldDefinitionType(
        Core.Entities.CustomFieldDefinition entity
    )
    {
        return new CustomFieldDefinitionType
        {
            Id = entity.Id,
            Key = entity.Key,
            Label = entity.Label,
            Widget = entity.Widget,
            ApplicableMetadataTypes = entity.ApplicableMetadataTypes,
            SortOrder = entity.SortOrder,
            IsEnabled = entity.IsEnabled,
        };
    }
}
