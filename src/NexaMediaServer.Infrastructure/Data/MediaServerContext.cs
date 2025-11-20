// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Reflection;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Infrastructure.Data.Interceptors;

namespace NexaMediaServer.Infrastructure.Data;

/// <summary>
/// Represents the database context for the media server.
/// </summary>
public class MediaServerContext : IdentityDbContext<User>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MediaServerContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    public MediaServerContext(DbContextOptions<MediaServerContext> options)
        : base(options) { }

    /// <summary>
    /// Gets the DbSet for metadata items.
    /// </summary>
    public DbSet<MetadataItem> MetadataItems => this.Set<MetadataItem>();

    /// <summary>
    /// Gets the DbSet for metadata item settings.
    /// </summary>
    public DbSet<MetadataItemSetting> MetadataItemSettings => this.Set<MetadataItemSetting>();

    /// <summary>
    /// Gets the DbSet for media items.
    /// </summary>
    public DbSet<MediaItem> MediaItems => this.Set<MediaItem>();

    /// <summary>
    /// Gets the DbSet for media parts.
    /// </summary>
    public DbSet<MediaPart> MediaParts => this.Set<MediaPart>();

    /// <summary>
    /// Gets the DbSet for libraries.
    /// </summary>
    public DbSet<LibrarySection> LibrarySections => this.Set<LibrarySection>();

    /// <summary>
    /// Gets the DbSet for library folders.
    /// </summary>
    public DbSet<SectionLocation> SectionsLocations => this.Set<SectionLocation>();

    /// <summary>
    /// Gets the DbSet for directories.
    /// </summary>
    public DbSet<Core.Entities.Directory> Directories => this.Set<Core.Entities.Directory>();

    /// <summary>
    /// Gets the DbSet for server settings.
    /// </summary>
    public DbSet<ServerSetting> ServerSettings => this.Set<ServerSetting>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
