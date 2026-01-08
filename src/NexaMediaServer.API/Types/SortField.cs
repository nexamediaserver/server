// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Represents an available sort field option for browsing a library section.
/// </summary>
[GraphQLName("SortField")]
public sealed class SortField
{
    /// <summary>
    /// Gets the unique key identifier for this sort field.
    /// This key should be used when constructing sort input objects.
    /// </summary>
    public string Key { get; init; } = null!;

    /// <summary>
    /// Gets the user-facing display name for this sort field.
    /// </summary>
    public string DisplayName { get; init; } = null!;

    /// <summary>
    /// Gets a value indicating whether sorting by this field requires user-specific data.
    /// When true, sorting uses a SQL join to user data (e.g., Progress, Date Viewed, Plays).
    /// When false, sorting can be performed efficiently without additional joins.
    /// </summary>
    public bool RequiresUserData { get; init; }
}
