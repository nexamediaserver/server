// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;
using NexaMediaServer.Infrastructure.Services.Resolvers;

namespace NexaMediaServer.Infrastructure.Services.Metadata;

/// <summary>
/// Request payload for embedded metadata extraction.
/// </summary>
/// <param name="MediaFile">The media file to inspect.</param>
/// <param name="LibraryType">Library context.</param>
public sealed record EmbeddedMetadataRequest(FileSystemMetadata MediaFile, LibraryType LibraryType);
