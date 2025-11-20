// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Core.Services;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods to register infrastructure services for the application.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers infrastructure services required by the application, including database context, identity, interceptors, and authorization policies.
    /// </summary>
    /// <param name="builder">The application host builder to configure services for.</param>
    public static void AddCoreServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<ILibrarySectionService, LibrarySectionService>();
        builder.Services.AddScoped<IMetadataItemService, MetadataItemService>();
        builder.Services.AddScoped<IMediaItemService, MediaItemService>();
        builder.Services.AddScoped<IMediaPartService, MediaPartService>();
        builder.Services.AddScoped<IDirectoryService, DirectoryService>();
        builder.Services.AddScoped<ISectionLocationService, SectionLocationService>();
        builder.Services.AddScoped<ILibraryScanService, LibraryScanService>();
    }
}
