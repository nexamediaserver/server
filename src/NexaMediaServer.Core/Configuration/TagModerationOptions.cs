// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Configuration;

/// <summary>
/// Configuration options for tag moderation.
/// </summary>
public class TagModerationOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "TagModeration";

    /// <summary>
    /// Gets or sets the list of allowed tags.
    /// </summary>
    /// <remarks>
    /// If this list contains any entries, ONLY tags in this list are allowed (blocklist is ignored).
    /// If empty, tags are checked against the blocklist instead.
    /// </remarks>
    public List<string> AllowedTags { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of blocked tags.
    /// </summary>
    /// <remarks>
    /// Only used when AllowedTags is empty. Tags matching entries in this list are rejected.
    /// If both AllowedTags and BlockedTags are empty, no moderation is applied.
    /// </remarks>
    public List<string> BlockedTags { get; set; } = [];
}
