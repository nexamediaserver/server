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
public sealed record SidecarParseResult(
    MetadataBaseItem? Metadata,
    IReadOnlyDictionary<string, object>? Hints
);
