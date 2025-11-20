// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Options;
using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Implementation of tag moderation service.
/// </summary>
public class TagModerationService : ITagModerationService
{
    private readonly TagModerationOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TagModerationService"/> class.
    /// </summary>
    /// <param name="options">The tag moderation options.</param>
    public TagModerationService(IOptions<TagModerationOptions> options)
    {
        this.options = options.Value;
    }

    /// <inheritdoc/>
    public bool IsTagAllowed(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        {
            return false;
        }

        // If allowlist has entries, use it exclusively (ignore blocklist)
        if (this.options.AllowedTags.Count > 0)
        {
            return this.options.AllowedTags.Contains(tagName, StringComparer.OrdinalIgnoreCase);
        }

        // If blocklist has entries, reject matching tags
        if (this.options.BlockedTags.Count > 0)
        {
            return !this.options.BlockedTags.Contains(tagName, StringComparer.OrdinalIgnoreCase);
        }

        // No moderation configured - allow all tags
        return true;
    }
}
