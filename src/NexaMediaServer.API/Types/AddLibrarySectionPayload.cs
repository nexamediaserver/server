// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Represents the mutation payload returned after creating a library section.
/// </summary>
public sealed class AddLibrarySectionPayload
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddLibrarySectionPayload"/> class.
    /// </summary>
    /// <param name="librarySection">The created library section.</param>
    /// <param name="scanId">The queued scan identifier.</param>
    public AddLibrarySectionPayload(Core.Entities.LibrarySection librarySection, int scanId)
    {
        this.LibrarySection = new LibrarySection
        {
            Id = librarySection.Uuid,
            Name = librarySection.Name,
            SortName = librarySection.SortName,
            Type = librarySection.Type,
            Locations = librarySection.Locations.Select(loc => loc.RootPath).ToList(),
            Settings = LibrarySection.MapSettings(librarySection.Settings),
        };
        this.ScanId = scanId;
    }

    /// <summary>
    /// Gets the created library section.
    /// </summary>
    public LibrarySection LibrarySection { get; }

    /// <summary>
    /// Gets the identifier of the queued scan.
    /// </summary>
    public int ScanId { get; }
}
