// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs;

/// <summary>
/// Represents a checkpoint for resumable scan operations.
/// </summary>
/// <param name="Stage">The pipeline stage name when the checkpoint was created.</param>
/// <param name="Cursor">An opaque cursor value for resuming the stage (e.g., last processed path).</param>
/// <param name="Version">A monotonically increasing version for optimistic concurrency.</param>
public sealed record ScanCheckpoint(string Stage, string? Cursor, int Version);
