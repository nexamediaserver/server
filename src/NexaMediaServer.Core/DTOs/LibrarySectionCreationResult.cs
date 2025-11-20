// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.DTOs;

/// <summary>
/// Represents the result of creating a library section and scheduling its scan.
/// </summary>
public sealed record LibrarySectionCreationResult(LibrarySection LibrarySection, int ScanId);
