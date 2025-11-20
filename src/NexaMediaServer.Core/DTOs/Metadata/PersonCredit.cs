// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.DTOs.Metadata;

/// <summary>
/// Describes a person contribution to another metadata item.
/// </summary>
/// <param name="Person">The person metadata entry to link or create.</param>
/// <param name="RelationType">The relation type describing how the person is connected to the target item.</param>
/// <param name="Text">Optional free-form text (e.g., role name).</param>
public sealed record PersonCredit(Person Person, RelationType RelationType, string? Text = null);
