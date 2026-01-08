// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;

using HotChocolate.Authorization;

using NexaMediaServer.API.Types.Fields;
using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.DTOs.Fields;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Admin mutations for managing detail field configurations.
/// </summary>
public static partial class Mutation
{
    /// <summary>
    /// Updates the admin-defined detail field configuration for a metadata type and optional library.
    /// </summary>
    /// <param name="input">The configuration update request.</param>
    /// <param name="fieldService">The detail field service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated configuration.</returns>
    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    public static async Task<DetailFieldConfigurationType> UpdateAdminDetailFieldConfigurationAsync(
        UpdateAdminDetailFieldConfigurationInput input,
        [Service] IDetailFieldService fieldService,
        CancellationToken cancellationToken
    )
    {
        var configuration = new DetailFieldConfiguration(
            input.EnabledFieldTypes ?? Array.Empty<DetailFieldType>(),
            input.DisabledFieldTypes ?? Array.Empty<DetailFieldType>(),
            input.DisabledCustomFieldKeys ?? Array.Empty<string>(),
            input.FieldGroups?.Select(g => new DetailFieldGroup(
                g.GroupKey,
                g.Label,
                g.LayoutType,
                g.SortOrder,
                g.IsCollapsible
            )).ToList(),
            input.FieldGroupAssignments
        );

        await fieldService.UpdateAdminFieldConfigurationAsync(
            input.MetadataType,
            input.LibrarySectionId,
            configuration,
            cancellationToken
        );

        return new DetailFieldConfigurationType
        {
            MetadataType = input.MetadataType,
            LibrarySectionId = input.LibrarySectionId,
            EnabledFieldTypes = configuration.EnabledFieldTypes,
            DisabledFieldTypes = configuration.DisabledFieldTypes,
            DisabledCustomFieldKeys = configuration.DisabledCustomFieldKeys,
            FieldGroups = configuration.FieldGroups?.Select(g => new DetailFieldGroupType
            {
                GroupKey = g.GroupKey,
                Label = g.Label,
                LayoutType = g.LayoutType,
                SortOrder = g.SortOrder,
                IsCollapsible = g.IsCollapsible,
            }).ToList(),
            FieldGroupAssignments = configuration.FieldGroupAssignments,
        };
    }
}
