// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Infrastructure.Services.Metadata;
using NexaMediaServer.Infrastructure.Services.Resolvers;

namespace NexaMediaServer.Infrastructure.Services.Pipeline;

/// <summary>
/// Mutable work item flowing through the scan pipeline stages.
/// </summary>
public sealed record ScanWorkItem
{
    /// <summary>
    /// Gets the library location that yielded this item.
    /// </summary>
    public required SectionLocation Location { get; init; }

    /// <summary>
    /// Gets the filesystem entry being processed.
    /// </summary>
    public required FileSystemMetadata File { get; init; }

    /// <summary>
    /// Gets optional pre-fetched children (for directories).
    /// </summary>
    public IReadOnlyList<FileSystemMetadata>? Children { get; init; }

    /// <summary>
    /// Gets the sibling files in the same directory (for sidecar lookup).
    /// </summary>
    public IReadOnlyList<FileSystemMetadata>? Siblings { get; init; }

    /// <summary>
    /// Gets the ancestor chain from root to parent.
    /// </summary>
    public IReadOnlyList<AncestorInfo>? Ancestors { get; init; }

    /// <summary>
    /// Gets the resolved parent metadata, if already known.
    /// </summary>
    public MetadataBaseItem? ResolvedParent { get; init; }

    /// <summary>
    /// Gets optional loose hints that stages can enrich (e.g., identifiers).
    /// </summary>
    public IReadOnlyDictionary<string, object>? Hints { get; init; }

    /// <summary>
    /// Gets the resolved metadata DTO produced by resolver/merge stages.
    /// </summary>
    public MetadataBaseItem? ResolvedMetadata { get; init; }

    /// <summary>
    /// Gets the parsed sidecar result, if any.
    /// </summary>
    public SidecarParseResult? Sidecar { get; init; }

    /// <summary>
    /// Gets the extracted embedded metadata, if any.
    /// </summary>
    public EmbeddedMetadataResult? Embedded { get; init; }

    /// <summary>
    /// Gets a value indicating whether this item corresponds to the location root.
    /// </summary>
    public bool IsRoot { get; init; }

    /// <summary>
    /// Gets a value indicating whether the item was detected as unchanged and can be skipped.
    /// </summary>
    public bool IsUnchanged { get; init; }
}
