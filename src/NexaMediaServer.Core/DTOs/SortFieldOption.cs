// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs;

/// <summary>
/// Represents an available sort field option for browsing a library section.
/// </summary>
/// <param name="Key">The unique key identifier for this sort field.</param>
/// <param name="DisplayName">The user-facing display name for this sort field.</param>
/// <param name="RequiresUserDataJoin">
/// Indicates whether sorting by this field requires a SQL join to user-specific data.
/// When true, the query must be executed with a direct SQL join rather than in-memory sorting.
/// </param>
public readonly record struct SortFieldOption(
    string Key,
    string DisplayName,
    bool RequiresUserDataJoin
);
