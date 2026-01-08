// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;

namespace NexaMediaServer.API.Types.Fields;

/// <summary>
/// Represents a key-value pair for extra fields in the GraphQL API.
/// </summary>
[GraphQLName("ExtraField")]
public sealed class ExtraFieldType
{
    /// <summary>
    /// Gets the field key.
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Gets the field value as a JSON element.
    /// </summary>
    /// <remarks>
    /// The value can be a string, number, boolean, array, or object.
    /// Clients should interpret the value based on the corresponding
    /// <see cref="CustomFieldDefinitionType.Widget"/> type.
    /// </remarks>
    [GraphQLType(typeof(AnyType))]
    public JsonElement Value { get; init; }
}
