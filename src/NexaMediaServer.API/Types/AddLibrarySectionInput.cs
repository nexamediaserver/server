// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Represents the input required to create a library section.
/// </summary>
public sealed class AddLibrarySectionInput
{
    /// <summary>
    /// Gets or sets the library name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the library type.
    /// </summary>
    public LibraryType Type { get; set; }

    /// <summary>
    /// Gets or sets the root paths associated with the library.
    /// </summary>
    public List<string> RootPaths { get; set; } = new();

    /// <summary>
    /// Gets or sets the initial settings for the library section.
    /// </summary>
    public LibrarySectionSettings? Settings { get; set; }
}
