// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;

using HotChocolate.Authorization;

using NexaMediaServer.API.Types.Fields;
using NexaMediaServer.API.Types.Hubs;
using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.DTOs.Hubs;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Admin configuration queries for hubs and detail fields.
/// </summary>
public static partial class Query
{
    /// <summary>
    /// Retrieves a hub configuration for the given context and scope (admin only).
    /// </summary>
    /// <param name="input">The configuration scope.</param>
    /// <param name="hubService">The hub service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stored hub configuration if found; otherwise null.</returns>
    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    public static async Task<HubConfigurationType?> GetHubConfigurationAsync(
        HubConfigurationScopeInput input,
        [Service] IHubService hubService,
        CancellationToken cancellationToken
    )
    {
        ValidateHubScope(input.Context, input.LibrarySectionId, input.MetadataType);

        var configuration = await hubService.GetHubConfigurationAsync(
            input.Context,
            input.LibrarySectionId,
            input.MetadataType,
            cancellationToken
        );

        return configuration == null ? null : MapHubConfiguration(configuration);
    }

    /// <summary>
    /// Retrieves the admin-defined detail field configuration for a metadata type and optional library scope (admin only).
    /// </summary>
    /// <param name="input">The configuration scope.</param>
    /// <param name="fieldService">The detail field service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stored configuration if found; otherwise null.</returns>
    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    public static async Task<DetailFieldConfigurationType?> GetAdminDetailFieldConfigurationAsync(
        DetailFieldConfigurationScopeInput input,
        [Service] IDetailFieldService fieldService,
        CancellationToken cancellationToken
    )
    {
        var configuration = await fieldService.GetAdminFieldConfigurationAsync(
            input.MetadataType,
            input.LibrarySectionId,
            cancellationToken
        );

        if (configuration == null)
        {
            return null;
        }

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

    private static void ValidateHubScope(
        HubContext context,
        Guid? librarySectionId,
        MetadataType? metadataType
    )
    {
        switch (context)
        {
            case HubContext.Home:
                if (librarySectionId.HasValue || metadataType.HasValue)
                {
                    throw new ArgumentException(
                        "Home hub configuration cannot be scoped to library or metadata type."
                    );
                }

                break;
            case HubContext.LibraryDiscover:
                if (!librarySectionId.HasValue || metadataType.HasValue)
                {
                    throw new ArgumentException(
                        "Library discover hub configuration requires a library section and no metadata type."
                    );
                }

                break;
            case HubContext.ItemDetail:
                if (!metadataType.HasValue)
                {
                    throw new ArgumentException(
                        "Item detail hub configuration requires a metadata type."
                    );
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(context));
        }
    }

    private static HubConfigurationType MapHubConfiguration(HubConfiguration configuration) =>
        new()
        {
            EnabledHubTypes = configuration.EnabledHubTypes,
            DisabledHubTypes = configuration.DisabledHubTypes,
        };
}
