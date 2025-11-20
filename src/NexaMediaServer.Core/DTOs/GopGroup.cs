// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.DTOs;

/// <summary>
/// Represents a single Group-of-Pictures (GoP) entry in the GoP index.
/// </summary>
public sealed record GopGroup(long PtsMs, long DurationMs, long SizeBytes);
