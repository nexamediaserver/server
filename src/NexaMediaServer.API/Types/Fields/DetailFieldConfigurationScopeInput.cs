// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.API.Types.Fields;

/// <summary>
/// Input describing the scope of a detail field configuration lookup.
/// </summary>
[GraphQLName("DetailFieldConfigurationScopeInput")]
public sealed class DetailFieldConfigurationScopeInput
{
    /// <summary>
    /// Gets or sets the metadata type the configuration applies to.
    /// </summary>
    public MetadataType MetadataType { get; set; }

    /// <summary>
    /// Gets or sets the optional library section identifier for scoped overrides.
    /// </summary>
    [ID]
    public Guid? LibrarySectionId { get; set; }
}
