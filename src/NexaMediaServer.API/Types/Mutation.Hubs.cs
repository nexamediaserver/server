// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;

using HotChocolate.Authorization;

using NexaMediaServer.API.Types.Hubs;
using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.DTOs.Hubs;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL mutations for managing hub configurations.
/// </summary>
public static partial class Mutation
{
    /// <summary>
    /// Updates a hub configuration for the specified context and scope (admin only).
    /// </summary>
    /// <param name="input">The hub configuration update request.</param>
    /// <param name="hubService">The hub service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated hub configuration.</returns>
    [Authorize(Policy = AuthorizationPolicies.Administrator)]
    public static async Task<HubConfigurationType> UpdateHubConfigurationAsync(
        UpdateHubConfigurationInput input,
        [Service] IHubService hubService,
        CancellationToken cancellationToken
    )
    {
        ValidateHubScope(input.Context, input.LibrarySectionId, input.MetadataType);

        var configuration = new HubConfiguration(
            input.EnabledHubTypes ?? Array.Empty<HubType>(),
            input.DisabledHubTypes ?? Array.Empty<HubType>()
        );

        var updatedConfiguration = input.Context switch
        {
            HubContext.Home => await hubService.UpdateHomeHubConfigurationAsync(
                configuration,
                cancellationToken
            ),
            HubContext.LibraryDiscover => await hubService.UpdateLibraryHubConfigurationAsync(
                input.LibrarySectionId!.Value,
                configuration,
                cancellationToken
            ),
            HubContext.ItemDetail => await hubService.UpdateItemDetailHubConfigurationAsync(
                input.MetadataType!.Value,
                input.LibrarySectionId,
                configuration,
                cancellationToken
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(input), "Unsupported hub context."),
        };

        return MapHubConfiguration(updatedConfiguration);
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
