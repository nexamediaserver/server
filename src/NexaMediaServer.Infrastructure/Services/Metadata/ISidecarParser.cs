// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading;
using System.Threading.Tasks;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Infrastructure.Services.Resolvers;

namespace NexaMediaServer.Infrastructure.Services.Metadata;

/// <summary>
/// Parses sidecar files (e.g., .nfo, metadata.json) into structured metadata overrides.
/// Implementations are discovered via the parts registry.
/// </summary>
public interface ISidecarParser : IHasOrder
{
    /// <summary>
    /// Gets a unique identifier for this parser to use for logging and artifact naming.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a human-readable display name for the UI.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets a user-friendly description of what this parser does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the library types this parser supports.
    /// Return an empty collection if the parser supports all library types.
    /// </summary>
    IReadOnlyCollection<LibraryType> SupportedLibraryTypes { get; }

    /// <summary>
    /// Returns true when this parser can handle the provided sidecar file.
    /// </summary>
    /// <param name="sidecarFile">Sidecar file metadata.</param>
    /// <returns><c>true</c> when supported; otherwise <c>false</c>.</returns>
    bool CanParse(FileSystemMetadata sidecarFile);

    /// <summary>
    /// Parses the sidecar for the given media file.
    /// </summary>
    /// <param name="request">Parse request containing media and sidecar context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A metadata patch or null when not applicable.</returns>
    Task<SidecarParseResult?> ParseAsync(
        SidecarParseRequest request,
        CancellationToken cancellationToken
    );
}
