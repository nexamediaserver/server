// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;
using NexaMediaServer.Infrastructure.Services.Resolvers;

namespace NexaMediaServer.Infrastructure.Services.Metadata;

/// <summary>
/// Request payload for parsing a sidecar adjacent to a media file.
/// </summary>
/// <param name="MediaFile">The media file being processed.</param>
/// <param name="SidecarFile">The sidecar file to parse.</param>
/// <param name="LibraryType">The library type context.</param>
public sealed record SidecarParseRequest(
    FileSystemMetadata MediaFile,
    FileSystemMetadata SidecarFile,
    LibraryType LibraryType
);
