// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using NexaMediaServer.Core.DTOs.Metadata;

namespace NexaMediaServer.Infrastructure.Services.Metadata;

/// <summary>
/// Parsed sidecar result containing overrides and optional hints.
/// </summary>
/// <param name="Metadata">A metadata DTO containing overrides (may be partial).</param>
/// <param name="Hints">Optional loose hints (e.g., identifiers) for later stages.</param>
/// <param name="Source">Identifier of the parser that produced this result (for artifact naming).</param>
/// <param name="People">Optional person contributions discovered from the sidecar.</param>
/// <param name="Groups">Optional group contributions discovered from the sidecar.</param>
/// <param name="Genres">Optional genre names discovered from the sidecar.</param>
/// <param name="Tags">Optional tag names discovered from the sidecar.</param>
public sealed record SidecarParseResult(
    MetadataBaseItem? Metadata,
    IReadOnlyDictionary<string, object>? Hints,
    string Source,
    IReadOnlyList<PersonCredit>? People = null,
    IReadOnlyList<GroupCredit>? Groups = null,
    IReadOnlyList<string>? Genres = null,
    IReadOnlyList<string>? Tags = null
);
