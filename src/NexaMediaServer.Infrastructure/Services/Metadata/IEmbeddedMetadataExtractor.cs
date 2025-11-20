// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;
using NexaMediaServer.Infrastructure.Services.Resolvers;

namespace NexaMediaServer.Infrastructure.Services.Metadata;

/// <summary>
/// Extracts embedded metadata from media containers (e.g., ID3, Matroska tags, MP4 atoms).
/// Implementations are discovered via the parts registry.
/// </summary>
public interface IEmbeddedMetadataExtractor : IHasOrder
{
    /// <summary>
    /// Gets a unique identifier for this extractor to use for logging and artifact naming.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a human-readable display name for the UI.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets a user-friendly description of what this extractor does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the library types this extractor supports.
    /// Return an empty collection if the extractor supports all library types.
    /// </summary>
    IReadOnlyCollection<LibraryType> SupportedLibraryTypes { get; }

    /// <summary>
    /// Returns true when this extractor can handle the provided media file.
    /// </summary>
    /// <param name="mediaFile">Media file metadata.</param>
    /// <returns><c>true</c> when supported; otherwise <c>false</c>.</returns>
    bool CanExtract(FileSystemMetadata mediaFile);

    /// <summary>
    /// Extracts embedded metadata for the provided media file.
    /// </summary>
    /// <param name="request">Extraction request context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Extracted metadata patch or null when unsupported.</returns>
    Task<EmbeddedMetadataResult?> ExtractAsync(
        EmbeddedMetadataRequest request,
        CancellationToken cancellationToken
    );
}
