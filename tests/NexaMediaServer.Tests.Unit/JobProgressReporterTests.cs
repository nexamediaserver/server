// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NaturalSort.Extension;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Infrastructure.Data;
using NexaMediaServer.Infrastructure.Services;
using Xunit;

namespace NexaMediaServer.Tests.Unit;

/// <summary>
/// Unit tests for the <see cref="JobProgressReporter"/> class.
/// </summary>
public sealed class JobProgressReporterTests : IDisposable
{
    private static readonly NaturalSortComparer NaturalComparer = new(
        StringComparison.CurrentCultureIgnoreCase
    );

    private readonly SqliteConnection connection;
    private readonly MediaServerContext db;
    private readonly InMemorySqliteDbContextFactory dbContextFactory;
    private readonly JobProgressReporter sut;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobProgressReporterTests"/> class.
    /// </summary>
    public JobProgressReporterTests()
    {
        // Create and open a SQLite in-memory connection
        this.connection = new SqliteConnection("Data Source=:memory:");
        this.connection.Open();

        // Register the NATURALSORT collation before EnsureCreated
        this.connection.CreateCollation("NATURALSORT", (x, y) => NaturalComparer.Compare(x, y));

        var options = new DbContextOptionsBuilder<MediaServerContext>()
            .UseSqlite(this.connection)
            .Options;

        this.db = new MediaServerContext(options);
        this.db.Database.EnsureCreated();

        // Seed required data
        this.db.LibrarySections.Add(
            new LibrarySection
            {
                Id = 1,
                Name = "Test Movies",
                Type = LibraryType.Movies,
                CreatedAt = DateTime.UtcNow,
            }
        );
        this.db.SaveChanges();

        // Create factory mock using the same open connection
        this.dbContextFactory = new InMemorySqliteDbContextFactory(this.connection);

        this.sut = new JobProgressReporter(
            this.dbContextFactory,
            NullLogger<JobProgressReporter>.Instance
        );
    }

    /// <summary>
    /// Disposes the test resources.
    /// </summary>
    public void Dispose()
    {
        this.db.Dispose();
        this.connection.Dispose();
    }

    /// <summary>
    /// Tests that StartAsync creates a new entry with correct initial state.
    /// </summary>
    [Fact]
    public async Task StartAsyncCreatesNewEntryWithCorrectInitialState()
    {
        // Arrange
        const int librarySectionId = 1;
        const JobType jobType = JobType.LibraryScan;
        const int totalItems = 100;

        // Act
        await this.sut.StartAsync(librarySectionId, jobType, totalItems);

        // Assert
        await using var verifyDb = await this.dbContextFactory.CreateDbContextAsync();
        var entry = await verifyDb.JobNotificationEntries.FirstOrDefaultAsync(e =>
            e.LibrarySectionId == librarySectionId && e.JobType == jobType
        );

        entry.Should().NotBeNull();
        entry!.Status.Should().Be(JobNotificationStatus.Running);
        entry.TotalItems.Should().Be(totalItems);
        entry.CompletedItems.Should().Be(0);
        entry.Progress.Should().Be(0);
        entry.ErrorMessage.Should().BeNull();
    }

    /// <summary>
    /// Tests that ReportProgressAsync updates the entry correctly.
    /// </summary>
    [Fact]
    public async Task ReportProgressAsyncUpdatesEntryCorrectly()
    {
        // Arrange
        const int librarySectionId = 1;
        const JobType jobType = JobType.LibraryScan;
        await this.sut.StartAsync(librarySectionId, jobType, 100);

        // Act
        await this.sut.ReportProgressAsync(librarySectionId, jobType, 50, 100);

        // Assert
        await using var verifyDb = await this.dbContextFactory.CreateDbContextAsync();
        var entry = await verifyDb.JobNotificationEntries.FirstOrDefaultAsync(e =>
            e.LibrarySectionId == librarySectionId && e.JobType == jobType
        );

        entry.Should().NotBeNull();
        entry!.CompletedItems.Should().Be(50);
        entry.Progress.Should().Be(50);
    }

    /// <summary>
    /// Tests that CompleteAsync sets status to Completed.
    /// </summary>
    [Fact]
    public async Task CompleteAsyncSetsStatusToCompleted()
    {
        // Arrange
        const int librarySectionId = 1;
        const JobType jobType = JobType.MetadataRefresh;
        await this.sut.StartAsync(librarySectionId, jobType, 10);

        // Act
        await this.sut.CompleteAsync(librarySectionId, jobType);

        // Assert
        await using var verifyDb = await this.dbContextFactory.CreateDbContextAsync();
        var entry = await verifyDb.JobNotificationEntries.FirstOrDefaultAsync(e =>
            e.LibrarySectionId == librarySectionId && e.JobType == jobType
        );

        entry.Should().NotBeNull();
        entry!.Status.Should().Be(JobNotificationStatus.Completed);
        entry.Progress.Should().Be(100);
        entry.CompletedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that FailAsync sets status to Failed with error message.
    /// </summary>
    [Fact]
    public async Task FailAsyncSetsStatusToFailedWithErrorMessage()
    {
        // Arrange
        const int librarySectionId = 1;
        const JobType jobType = JobType.FileAnalysis;
        const string errorMessage = "Test error message";
        await this.sut.StartAsync(librarySectionId, jobType, 5);

        // Act
        await this.sut.FailAsync(librarySectionId, jobType, errorMessage);

        // Assert
        await using var verifyDb = await this.dbContextFactory.CreateDbContextAsync();
        var entry = await verifyDb.JobNotificationEntries.FirstOrDefaultAsync(e =>
            e.LibrarySectionId == librarySectionId && e.JobType == jobType
        );

        entry.Should().NotBeNull();
        entry!.Status.Should().Be(JobNotificationStatus.Failed);
        entry.ErrorMessage.Should().Be(errorMessage);
        entry.CompletedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that DrainPendingChanges returns and clears pending changes.
    /// </summary>
    [Fact]
    public async Task DrainPendingChangesReturnsAndClearsPendingChanges()
    {
        // Arrange
        await this.sut.StartAsync(1, JobType.LibraryScan, 100);
        await this.sut.ReportProgressAsync(1, JobType.LibraryScan, 50, 100);

        // Act - first drain
        var firstDrain = this.sut.DrainPendingChanges();

        // Assert
        firstDrain.Should().NotBeEmpty();
        firstDrain
            .Should()
            .ContainSingle(e => e.LibrarySectionId == 1 && e.JobType == JobType.LibraryScan);

        // Act - second drain should be empty
        var secondDrain = this.sut.DrainPendingChanges();
        secondDrain.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that PurgeHistoryAsync removes old completed entries.
    /// </summary>
    [Fact]
    public async Task PurgeHistoryAsyncRemovesOldCompletedEntries()
    {
        // Arrange
        await using var setupDb = await this.dbContextFactory.CreateDbContextAsync();

        // Add an old completed entry
        var oldEntry = new JobNotificationEntry
        {
            LibrarySectionId = 1,
            JobType = JobType.LibraryScan,
            Status = JobNotificationStatus.Completed,
            StartedAt = DateTime.UtcNow.AddDays(-10),
            CompletedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-10),
        };
        setupDb.JobNotificationEntries.Add(oldEntry);
        await setupDb.SaveChangesAsync();

        // Act
        var purged = await this.sut.PurgeHistoryAsync(retentionDays: 7);

        // Assert
        purged.Should().Be(1);

        await using var verifyDb = await this.dbContextFactory.CreateDbContextAsync();
        var remaining = await verifyDb.JobNotificationEntries.CountAsync();
        remaining.Should().Be(0);
    }

    /// <summary>
    /// Tests that GetLibraryNameAsync returns the correct name.
    /// </summary>
    [Fact]
    public async Task GetLibraryNameAsyncReturnsCorrectName()
    {
        // Act
        var name = await this.sut.GetLibraryNameAsync(1);

        // Assert
        name.Should().Be("Test Movies");
    }

    /// <summary>
    /// Tests that GetLibraryNameAsync caches the result.
    /// </summary>
    [Fact]
    public async Task GetLibraryNameAsyncCachesResult()
    {
        // Act - first call
        var name1 = await this.sut.GetLibraryNameAsync(1);

        // Act - second call should use cache
        var name2 = await this.sut.GetLibraryNameAsync(1);

        // Assert
        name1.Should().Be("Test Movies");
        name2.Should().Be("Test Movies");
    }

    /// <summary>
    /// Simple SQLite in-memory DbContext factory for testing.
    /// </summary>
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
}
