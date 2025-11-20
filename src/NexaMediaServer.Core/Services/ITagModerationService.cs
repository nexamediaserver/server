// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Service for moderating tags based on allowlist/blocklist configuration.
/// </summary>
public interface ITagModerationService
{
    /// <summary>
    /// Determines whether a tag is allowed based on moderation rules.
    /// </summary>
    /// <param name="tagName">The tag name to check.</param>
    /// <returns>True if the tag is allowed; otherwise, false.</returns>
    /// <remarks>
    /// If AllowedTags is populated, only tags in that list are allowed (blocklist ignored).
    /// If AllowedTags is empty, tags in BlockedTags are rejected.
    /// If both lists are empty, all tags are allowed.
    /// </remarks>
    bool IsTagAllowed(string tagName);
}
