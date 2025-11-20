// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.DTOs.Metadata;

/// <summary>
/// Describes a group contribution to another metadata item, optionally with member credits.
/// </summary>
/// <param name="Group">The group metadata entry to link or create.</param>
/// <param name="RelationType">The relation type describing how the group is connected to the target item.</param>
/// <param name="Text">Optional free-form text (e.g., role name).</param>
/// <param name="Members">Optional member credits describing the group's people.</param>
public sealed record GroupCredit(
    Group Group,
    RelationType RelationType,
    string? Text = null,
    IReadOnlyList<PersonCredit>? Members = null
);
