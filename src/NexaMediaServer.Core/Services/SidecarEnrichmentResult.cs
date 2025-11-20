// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Metadata;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Result container for sidecar/embedded local metadata enrichment.
/// </summary>
public sealed class SidecarEnrichmentResult
{
    /// <summary>
    /// Gets or sets a value indicating whether local metadata was applied to the item.
    /// </summary>
    public bool LocalMetadataApplied { get; set; }

    /// <summary>
    /// Gets or sets person credits extracted from local metadata.
    /// </summary>
    public List<PersonCredit>? People { get; set; }

    /// <summary>
    /// Gets or sets group credits extracted from local metadata.
    /// </summary>
    public List<GroupCredit>? Groups { get; set; }

    /// <summary>
    /// Gets or sets genre names extracted from local metadata.
    /// </summary>
    public List<string>? Genres { get; set; }

    /// <summary>
    /// Gets or sets tag names extracted from local metadata.
    /// </summary>
    public List<string>? Tags { get; set; }
}
