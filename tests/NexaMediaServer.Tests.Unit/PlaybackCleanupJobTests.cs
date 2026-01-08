// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.IO;
using System.Linq;

using FluentAssertions;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using NaturalSort.Extension;

using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Data;
using NexaMediaServer.Infrastructure.Services;

using Xunit;

using FileDirectory = System.IO.Directory;

namespace NexaMediaServer.Tests.Unit;

/// <summary>
/// Unit tests for cleaning up expired playback sessions and DASH caches.
/// </summary>
public sealed class PlaybackCleanupJobTests : IDisposable
{
    private static readonly NaturalSortComparer NaturalComparer = new(
        StringComparison.OrdinalIgnoreCase);

    private readonly SqliteConnection connection;
    private readonly MediaServerContext db;
    private readonly InMemorySqliteDbContextFactory dbContextFactory;
    private readonly string cacheRoot;
    private string? dashPath;

    /// <summary>
    /// Initializes an in-memory database and temporary cache directory for tests.
    /// </summary>
    public PlaybackCleanupJobTests()
    {
        this.connection = new SqliteConnection("Data Source=:memory:");
        this.connection.Open();

        // Register the NATURALSORT collation before EnsureCreated
        this.connection.CreateCollation("NATURALSORT", (x, y) => NaturalComparer.Compare(x, y));

        var options = new DbContextOptionsBuilder<MediaServerContext>()
            .UseSqlite(this.connection)
            .Options;

        this.db = new MediaServerContext(options);
        this.db.Database.EnsureCreated();
        this.dbContextFactory = new InMemorySqliteDbContextFactory(this.connection);

        this.cacheRoot = Path.Combine(Path.GetTempPath(), "nexa-playback-tests", Guid.NewGuid().ToString("N"));
        FileDirectory.CreateDirectory(this.cacheRoot);

        this.SeedPlaybackSession();
    }

    /// <summary>
    /// Verifies that expired playback sessions and associated DASH cache are removed.
    /// </summary>
    [Fact]
    public async Task ExecuteAsyncRemovesExpiredPlaybackAndDashCache()
    {
        var paths = new FakeApplicationPaths(this.cacheRoot);
        var job = new PlaybackCleanupJob(
            this.dbContextFactory,
            paths,
            NullLogger<PlaybackCleanupJob>.Instance
        );

        await job.ExecuteAsync(CancellationToken.None);

        await using var verifyDb = await this.dbContextFactory.CreateDbContextAsync();
        (await verifyDb.PlaybackSessions.CountAsync()).Should().Be(0);
        (await verifyDb.PlaylistGenerators.CountAsync()).Should().Be(0);

        FileDirectory.Exists(this.dashPath!).Should().BeFalse();
    }

    private void SeedPlaybackSession()
    {
        var librarySection = new LibrarySection
        {
            Name = "Test Movies",
            Type = Core.Enums.LibraryType.Movies,
            CreatedAt = DateTime.UtcNow,
        };

        var sectionLocation = new SectionLocation
        {
            LibrarySection = librarySection,
            RootPath = "/tmp/test",
            Available = true,
            LastScannedAt = DateTime.UtcNow,
        };

        var user = new User
        {
            Id = "user-1",
            UserName = "user1",
            NormalizedUserName = "USER1",
            Email = "user1@example.com",
            NormalizedEmail = "USER1@EXAMPLE.COM",
            CreatedAt = DateTime.UtcNow,
        };

        var device = new Device
        {
            Identifier = "device-1",
            Name = "Test Device",
            Platform = "web",
            User = user,
            UserId = user.Id,
        };

        this.db.LibrarySections.Add(librarySection);
        this.db.SectionsLocations.Add(sectionLocation);
        this.db.Users.Add(user);
        this.db.Devices.Add(device);
        this.db.SaveChanges();

        var session = new Session
        {
            User = user,
            UserId = user.Id,
            Device = device,
            DeviceId = device.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            PublicId = Guid.NewGuid(),
        };

        var metadata = new MetadataItem
        {
            Title = "Test Movie",
            SortTitle = "Test Movie",
            MetadataType = Core.Enums.MetadataType.Movie,
            Uuid = Guid.NewGuid(),
            LibrarySection = librarySection,
        };

        var mediaItem = new MediaItem
        {
            MetadataItem = metadata,
            SectionLocation = sectionLocation,
            Duration = TimeSpan.FromMinutes(90),
        };

        var mediaPart = new MediaPart
        {
            MediaItem = mediaItem,
            File = "/tmp/test.mp4",
        };

        var capabilityProfile = new CapabilityProfile
        {
            Session = session,
            Version = 1,
            Capabilities = new Core.DTOs.Playback.PlaybackCapabilities(),
            DeclaredAt = DateTime.UtcNow,
        };

        var playback = new PlaybackSession
        {
            Session = session,
            CapabilityProfile = capabilityProfile,
            CurrentMetadataItem = metadata,
            CurrentMediaPart = mediaPart,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5),
            LastHeartbeatAt = DateTime.UtcNow.AddHours(-1),
        };

        var generator = new PlaylistGenerator
        {
            PlaybackSession = playback,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5),
            ChunkSize = 10,
        };

        // Save in correct order to satisfy foreign key constraints
        this.db.Sessions.Add(session);
        this.db.SaveChanges();

        this.db.MetadataItems.Add(metadata);
        this.db.SaveChanges();

        this.db.MediaItems.Add(mediaItem);
        this.db.SaveChanges();

        this.db.MediaParts.Add(mediaPart);
        this.db.SaveChanges();

        this.db.CapabilityProfiles.Add(capabilityProfile);
        this.db.SaveChanges();

        this.db.PlaybackSessions.Add(playback);
        this.db.SaveChanges();

        this.db.PlaylistGenerators.Add(generator);
        this.db.SaveChanges();

        // Create dash cache folder matching part index 0
        var dashPath = Path.Combine(
            this.cacheRoot,
            "dash",
            metadata.Uuid.ToString("N"),
            "0"
        );
        FileDirectory.CreateDirectory(dashPath);

        // Set the directory's last write time to be older than 5 minutes
        // so it can be cleaned up by the job
        var dirInfo = new DirectoryInfo(dashPath);
        dirInfo.LastWriteTimeUtc = DateTime.UtcNow.AddMinutes(-10);

        this.dashPath = dashPath;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        this.db.Dispose();
        this.connection.Dispose();
        if (FileDirectory.Exists(this.cacheRoot))
        {
            FileDirectory.Delete(this.cacheRoot, recursive: true);
        }
    }

    private sealed class InMemorySqliteDbContextFactory : IDbContextFactory<MediaServerContext>
    {
        private readonly SqliteConnection connection;

        public InMemorySqliteDbContextFactory(SqliteConnection connection)
        {
            this.connection = connection;
        }

        public MediaServerContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<MediaServerContext>()
                .UseSqlite(this.connection)
                .Options;

            return new MediaServerContext(options);
        }

        public Task<MediaServerContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default
        )
        {
            return Task.FromResult(this.CreateDbContext());
        }
    }

    private sealed class FakeApplicationPaths : IApplicationPaths
    {
        public FakeApplicationPaths(string cacheRoot)
        {
            this.CacheDirectory = cacheRoot;
        }

        public string DataDirectory => this.CacheDirectory;

        public string ConfigDirectory => this.CacheDirectory;

        public string LogDirectory => this.CacheDirectory;

        public string CacheDirectory { get; }

        public string MediaDirectory => this.CacheDirectory;

        public string TempDirectory => this.CacheDirectory;

        public string DatabaseDirectory => this.CacheDirectory;

        public string IndexDirectory => this.CacheDirectory;

        public string BackupDirectory => this.CacheDirectory;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public void EnsureDirectoryExists(string path)
        {
            if (!FileDirectory.Exists(path))
            {
                FileDirectory.CreateDirectory(path);
            }
        }

        public string GetDataPath(params string[] paths)
        {
            return Path.Combine(new[] { this.CacheDirectory }.Concat(paths).ToArray());
        }
    }
}
