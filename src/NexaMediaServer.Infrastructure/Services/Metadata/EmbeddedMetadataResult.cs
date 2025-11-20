// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using NexaMediaServer.Core.DTOs.Metadata;

namespace NexaMediaServer.Infrastructure.Services.Metadata;

/// <summary>
/// Result payload from embedded metadata extraction.
/// </summary>
/// <param name="Metadata">A metadata DTO containing overrides (may be partial).</param>
/// <param name="Hints">Optional loose hints (e.g., identifiers, language codes).</param>
public sealed record EmbeddedMetadataResult(
    MetadataBaseItem? Metadata,
    IReadOnlyDictionary<string, object>? Hints
);
