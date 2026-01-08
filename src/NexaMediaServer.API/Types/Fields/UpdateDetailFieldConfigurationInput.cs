// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.API.Types.Fields;

/// <summary>
/// Input type for updating user field configuration.
/// </summary>
[GraphQLName("UpdateDetailFieldConfigurationInput")]
public sealed class UpdateDetailFieldConfigurationInput
{
    /// <summary>
    /// Gets or sets the metadata type this configuration applies to.
    /// </summary>
    public MetadataType MetadataType { get; set; }

    /// <summary>
    /// Gets or sets the list of enabled field types in display order.
    /// </summary>
    public IReadOnlyList<DetailFieldType>? EnabledFieldTypes { get; set; }

    /// <summary>
    /// Gets or sets the list of explicitly disabled field types.
    /// </summary>
    public IReadOnlyList<DetailFieldType>? DisabledFieldTypes { get; set; }
}
