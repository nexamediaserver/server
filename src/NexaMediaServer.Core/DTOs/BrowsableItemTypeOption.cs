// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Core.DTOs;

/// <summary>
/// Represents an available root item type option for browsing a library section.
/// </summary>
/// <param name="DisplayName">The user-facing display name for this item type.</param>
/// <param name="MetadataTypes">The metadata types that this option represents.</param>
public readonly record struct BrowsableItemTypeOption(
    string DisplayName,
    MetadataType[] MetadataTypes
);
