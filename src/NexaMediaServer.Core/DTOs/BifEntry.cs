// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs;

/// <summary>
/// Represents a single entry in a BIF (Base Index Frames) file.
/// </summary>
/// <param name="TimestampMs">The timestamp in milliseconds for this frame.</param>
/// <param name="Offset">The byte offset in the BIF file where the image data starts.</param>
public sealed record BifEntry(int TimestampMs, int Offset);
