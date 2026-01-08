// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;

namespace NexaMediaServer.API.Types.Fields;

/// <summary>
/// Input type for setting an extra field value on a metadata item.
/// </summary>
[GraphQLName("ExtraFieldInput")]
public sealed class ExtraFieldInput
{
    /// <summary>
    /// Gets or sets the field key.
    /// </summary>
    public string Key { get; set; } = null!;

    /// <summary>
    /// Gets or sets the field value as a JSON element.
    /// </summary>
    /// <remarks>
    /// The value can be a string, number, boolean, array, or object.
    /// </remarks>
    [GraphQLType(typeof(AnyType))]
    public JsonElement Value { get; set; }
}
