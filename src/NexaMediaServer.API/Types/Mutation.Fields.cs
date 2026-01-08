// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Claims;

using HotChocolate.Authorization;

using NexaMediaServer.API.Types.Fields;
using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.DTOs.Fields;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Defines GraphQL mutation operations for detail fields.
/// </summary>
public static partial class Mutation
{
    /// <summary>
    /// Updates the field visibility configuration for the current user.
    /// </summary>
    /// <param name="input">The configuration input.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor for getting the current user.</param>
    /// <param name="fieldService">The field service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated field definitions.</returns>
    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    public static async Task<IEnumerable<DetailFieldDefinitionType>> UpdateDetailFieldConfigurationAsync(
        UpdateDetailFieldConfigurationInput input,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] IDetailFieldService fieldService,
        CancellationToken cancellationToken
    )
    {
        var userId =
            httpContextAccessor.HttpContext?.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User not authenticated");

        var configuration = new DetailFieldConfiguration(
            input.EnabledFieldTypes ?? [],
            input.DisabledFieldTypes ?? [],
            []
        );

        await fieldService.UpdateUserFieldConfigurationAsync(
            userId,
            input.MetadataType,
            configuration,
            cancellationToken
        );

        // Return the updated field definitions
        var definitions = await fieldService.GetFieldDefinitionsForTypeAsync(
            input.MetadataType,
            userId,
            cancellationToken
        );

        return definitions.Select(d => new DetailFieldDefinitionType
        {
            Key = d.FieldType == DetailFieldType.Custom
                ? d.CustomFieldKey ?? string.Empty
                : d.FieldType.ToString(),
            FieldType = d.FieldType,
            Label = d.Label,
            Widget = d.Widget,
            SortOrder = d.SortOrder,
            CustomFieldKey = d.CustomFieldKey,
        });
    }

    /// <summary>
    /// Creates a new custom field definition (admin only).
    /// </summary>
    /// <param name="input">The custom field definition input.</param>
    /// <param name="fieldService">The field service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created custom field definition.</returns>
    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    public static async Task<CustomFieldDefinitionType> CreateCustomFieldDefinitionAsync(
        CreateCustomFieldDefinitionInput input,
        [Service] IDetailFieldService fieldService,
        CancellationToken cancellationToken
    )
    {
        var customField = await fieldService.CreateCustomFieldDefinitionAsync(
            input.Key,
            input.Label,
            input.Widget,
            input.ApplicableMetadataTypes ?? [],
            input.SortOrder,
            cancellationToken
        );

        return new CustomFieldDefinitionType
        {
            Id = customField.Id,
            Key = customField.Key,
            Label = customField.Label,
            Widget = customField.Widget,
            ApplicableMetadataTypes = customField.ApplicableMetadataTypes,
            SortOrder = customField.SortOrder,
            IsEnabled = customField.IsEnabled,
        };
    }

    /// <summary>
    /// Updates an existing custom field definition (admin only).
    /// </summary>
    /// <param name="input">The custom field definition update input.</param>
    /// <param name="fieldService">The field service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated custom field definition.</returns>
    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    public static async Task<CustomFieldDefinitionType> UpdateCustomFieldDefinitionAsync(
        UpdateCustomFieldDefinitionInput input,
        [Service] IDetailFieldService fieldService,
        CancellationToken cancellationToken
    )
    {
        var customField = await fieldService.UpdateCustomFieldDefinitionAsync(
            input.Id,
            input.Label,
            input.Widget,
            input.ApplicableMetadataTypes,
            input.SortOrder,
            input.IsEnabled,
            cancellationToken
        );

        return new CustomFieldDefinitionType
        {
            Id = customField.Id,
            Key = customField.Key,
            Label = customField.Label,
            Widget = customField.Widget,
            ApplicableMetadataTypes = customField.ApplicableMetadataTypes,
            SortOrder = customField.SortOrder,
            IsEnabled = customField.IsEnabled,
        };
    }

    /// <summary>
    /// Deletes a custom field definition (admin only).
    /// </summary>
    /// <param name="id">The ID of the custom field definition to delete.</param>
    /// <param name="fieldService">The field service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the field was deleted, false if it was not found.</returns>
    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    public static async Task<bool> DeleteCustomFieldDefinitionAsync(
        [ID] int id,
        [Service] IDetailFieldService fieldService,
        CancellationToken cancellationToken
    )
    {
        return await fieldService.DeleteCustomFieldDefinitionAsync(id, cancellationToken);
    }
}
