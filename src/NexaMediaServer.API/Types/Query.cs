// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading;
using HotChocolate.Authorization;
using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.Exceptions;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Services;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Defines GraphQL query operations for the API.
/// </summary>
[QueryType]
public static partial class Query
{
    /// <summary>
    /// Gets basic server information like version and environment.
    /// </summary>
    /// <param name="env">The hosting environment.</param>
    /// <returns>The server info object.</returns>
    public static ServerInfo GetServerInfo([Service] IWebHostEnvironment env)
    {
        // Prefer informational version; fallback to assembly version.
        var assembly = typeof(Query).Assembly;
        var informationalVersion = assembly
            .GetCustomAttributes(
                typeof(System.Reflection.AssemblyInformationalVersionAttribute),
                false
            )
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()
            ?.InformationalVersion;

        var version = informationalVersion ?? assembly.GetName().Version?.ToString() ?? "0.0.0";

        return new ServerInfo
        {
            VersionString = version,
            IsDevelopment = string.Equals(
                env.EnvironmentName,
                "Development",
                StringComparison.OrdinalIgnoreCase
            ),
        };
    }

    /// <summary>
    /// Gets a library section by its global Relay ID.
    /// </summary>
    /// <param name="id">The Relay global ID for LibrarySection.</param>
    /// <param name="service">The library section service.</param>
    /// <returns>A single LibrarySection.</returns>
    [Authorize]
    [UseFirstOrDefault]
    public static IQueryable<LibrarySection> GetLibrarySection(
        [ID] Guid id,
        [Service] ILibrarySectionService service
    ) =>
        service
            .GetQueryable()
            .Where(l => l.Uuid == id)
            .Select(l => new LibrarySection
            {
                Id = l.Uuid,
                Name = l.Name,
                Type = l.Type,
                SortName = l.SortName ?? string.Empty,
                Locations = l.Locations.Select(loc => loc.RootPath).ToList(),
            });

    /// <summary>
    /// Gets a paginated list of library sections.
    /// </summary>
    /// <param name="service">The library section service.</param>
    /// <returns>A connection of LibrarySections.</returns>
    [Authorize]
    [UsePaging(IncludeTotalCount = true, MaxPageSize = 50, DefaultPageSize = 50)]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<LibrarySection> GetLibrarySections(
        [Service] ILibrarySectionService service
    ) =>
        service
            .GetQueryable()
            .Select(l => new LibrarySection
            {
                Id = l.Uuid,
                Name = l.Name,
                Type = l.Type,
                SortName = l.SortName ?? string.Empty,
                Locations = l.Locations.Select(loc => loc.RootPath).ToList(),
            });

    /// <summary>
    /// Gets a metadata item.
    /// </summary>
    /// <param name="id">The identifier of the metadata item.</param>
    /// <param name="service">The metadata item service.</param>
    /// <returns>A metadata item instance.</returns>
    [Authorize]
    [UseFirstOrDefault]
    public static IQueryable<MetadataItem> GetMetadataItem(
        [ID] Guid id,
        [Service] IMetadataService service
    ) => service.GetQueryable().Where(m => m.Uuid == id).Select(MetadataMappings.ToApiType);

    /// <summary>
    /// Gets a collection of metadata items.
    /// </summary>
    /// <param name="service">The metadata item service.</param>
    /// <returns>A collection of metadata items.</returns>
    [Authorize]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static IQueryable<MetadataItem> GetMetadataItems([Service] IMetadataService service) =>
        service.GetQueryable().Select(MetadataMappings.ToApiType);

    /// <summary>
    /// Lists filesystem roots (drives, mounts) that can be browsed for library creation.
    /// </summary>
    /// <param name="browserService">The filesystem browser service.</param>
    /// <param name="cancellationToken">Token to cancel the enumeration.</param>
    /// <returns>A collection of filesystem roots.</returns>
    [Authorize(Roles = new[] { Roles.Administrator })]
    public static async Task<IEnumerable<FileSystemRoot>> GetFileSystemRootsAsync(
        [Service] IFileSystemBrowserService browserService,
        CancellationToken cancellationToken
    )
    {
        var roots = await browserService.GetRootsAsync(cancellationToken);
        return roots.Select(FileSystemRoot.FromDto);
    }

    /// <summary>
    /// Browses a directory path, returning child entries, while ensuring access restrictions.
    /// </summary>
    /// <param name="path">The absolute path to inspect.</param>
    /// <param name="browserService">The filesystem browser service.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>The directory listing for the requested path.</returns>
    [Authorize(Roles = new[] { Roles.Administrator })]
    public static async Task<DirectoryListing> BrowseDirectoryAsync(
        string path,
        [Service] IFileSystemBrowserService browserService,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var listing = await browserService.GetDirectoryAsync(path, cancellationToken);
            return DirectoryListing.FromDto(listing);
        }
        catch (FileSystemBrowserException ex)
        {
            throw new GraphQLException(
                ErrorBuilder
                    .New()
                    .SetMessage(ex.Message)
                    .SetCode("FILE_SYSTEM_BROWSE_ERROR")
                    .Build()
            );
        }
    }
}
