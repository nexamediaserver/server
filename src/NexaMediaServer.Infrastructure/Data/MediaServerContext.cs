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
    /// Gets the DbSet for metadata relations.
    /// </summary>
    public DbSet<MetadataRelation> MetadataRelations => this.Set<MetadataRelation>();

    /// <summary>
    /// Gets the DbSet for genres.
    /// </summary>
    public DbSet<Genre> Genres => this.Set<Genre>();

    /// <summary>
    /// Gets the DbSet for tags.
    /// </summary>
    public DbSet<Tag> Tags => this.Set<Tag>();

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

    /// <summary>
    /// Gets the DbSet for registered devices.
    /// </summary>
    public DbSet<Device> Devices => this.Set<Device>();

    /// <summary>
    /// Gets the DbSet for user sessions.
    /// </summary>
    public DbSet<Session> Sessions => this.Set<Session>();

    /// <summary>
    /// Gets the DbSet for capability profiles.
    /// </summary>
    public DbSet<CapabilityProfile> CapabilityProfiles => this.Set<CapabilityProfile>();

    /// <summary>
    /// Gets the DbSet for playback sessions.
    /// </summary>
    public DbSet<PlaybackSession> PlaybackSessions => this.Set<PlaybackSession>();

    /// <summary>
    /// Gets the DbSet for playlist generators.
    /// </summary>
    public DbSet<PlaylistGenerator> PlaylistGenerators => this.Set<PlaylistGenerator>();

    /// <summary>
    /// Gets the DbSet for playlist generator items.
    /// </summary>
    public DbSet<PlaylistGeneratorItem> PlaylistGeneratorItems => this.Set<PlaylistGeneratorItem>();

    /// <summary>
    /// Gets the DbSet for library scans.
    /// </summary>
    public DbSet<LibraryScan> LibraryScans => this.Set<LibraryScan>();

    /// <summary>
    /// Gets the DbSet for library scan seen paths (for checkpoint persistence).
    /// </summary>
    public DbSet<LibraryScanSeenPath> LibraryScanSeenPaths => this.Set<LibraryScanSeenPath>();

    /// <summary>
    /// Gets the DbSet for job notification entries.
    /// </summary>
    public DbSet<JobNotificationEntry> JobNotificationEntries => this.Set<JobNotificationEntry>();

    /// <summary>
    /// Gets the DbSet for user hub configurations.
    /// </summary>
    public DbSet<UserHubConfiguration> UserHubConfigurations => this.Set<UserHubConfiguration>();

    /// <summary>
    /// Gets the DbSet for external identifiers.
    /// </summary>
    public DbSet<ExternalIdentifier> ExternalIdentifiers => this.Set<ExternalIdentifier>();

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Apply global query filters for soft-deletable entities.
        // Soft-deleted records (DeletedAt != null) are excluded from all queries by default.
        // Use .IgnoreQueryFilters() when you need to include deleted records.
        builder.Entity<MetadataItem>().HasQueryFilter(e => e.DeletedAt == null);
        builder.Entity<MediaItem>().HasQueryFilter(e => e.DeletedAt == null);
        builder.Entity<MediaPart>().HasQueryFilter(e => e.DeletedAt == null);
        builder.Entity<Core.Entities.Directory>().HasQueryFilter(e => e.DeletedAt == null);
    }
}
