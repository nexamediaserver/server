// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Infrastructure.Data;
using Xunit;

namespace NexaMediaServer.Tests.Unit;

/// <summary>
/// Tests for the <see cref="BulkAuditTimestamps"/> helper class.
/// </summary>
public class BulkAuditTimestampsTests
{
    /// <summary>
    /// Verifies that ApplyInsertTimestamps sets both CreatedAt and UpdatedAt to valid UTC timestamps.
    /// </summary>
    [Fact]
    public void ApplyInsertTimestampsSetsCreatedAtAndUpdatedAt()
    {
        // Arrange
        var entities = new List<MetadataItem>
        {
            new()
            {
                Title = "Test 1",
                SortTitle = "Test 1",
                MetadataType = MetadataType.Movie,
            },
            new()
            {
                Title = "Test 2",
                SortTitle = "Test 2",
                MetadataType = MetadataType.Show,
            },
        };

        var beforeTime = DateTime.UtcNow;

        // Act
        BulkAuditTimestamps.ApplyInsertTimestamps(entities);

        var afterTime = DateTime.UtcNow;

        // Assert
        foreach (var entity in entities)
        {
            Assert.NotEqual(default, entity.CreatedAt);
            Assert.NotEqual(default, entity.UpdatedAt);
            Assert.True(entity.CreatedAt >= beforeTime && entity.CreatedAt <= afterTime);
            Assert.True(entity.UpdatedAt >= beforeTime && entity.UpdatedAt <= afterTime);
            Assert.Equal(entity.CreatedAt, entity.UpdatedAt);
        }
    }

    /// <summary>
    /// Verifies that all entities in a batch get the same timestamp.
    /// </summary>
    [Fact]
    public void ApplyInsertTimestampsUsesConsistentTimestamp()
    {
        // Arrange
        var entities = new List<MetadataItem>
        {
            new()
            {
                Title = "Test 1",
                SortTitle = "Test 1",
                MetadataType = MetadataType.Movie,
            },
            new()
            {
                Title = "Test 2",
                SortTitle = "Test 2",
                MetadataType = MetadataType.Show,
            },
            new()
            {
                Title = "Test 3",
                SortTitle = "Test 3",
                MetadataType = MetadataType.Episode,
            },
        };

        // Act
        BulkAuditTimestamps.ApplyInsertTimestamps(entities);

        // Assert - all entities should have the same timestamp (from single call to UtcNow)
        var firstCreatedAt = entities[0].CreatedAt;
        foreach (var entity in entities)
        {
            Assert.Equal(firstCreatedAt, entity.CreatedAt);
            Assert.Equal(firstCreatedAt, entity.UpdatedAt);
        }
    }

    /// <summary>
    /// Verifies that the helper works with different entity types inheriting from AuditableEntity.
    /// </summary>
    [Fact]
    public void ApplyInsertTimestampsWorksWithDifferentEntityTypes()
    {
        // Arrange - test with MediaItem (also inherits from SoftDeletableEntity -> AuditableEntity)
        var mediaItems = new List<MediaItem>
        {
            new() { MetadataItemId = 1, SectionLocationId = 1 },
            new() { MetadataItemId = 2, SectionLocationId = 1 },
        };

        var beforeTime = DateTime.UtcNow;

        // Act
        BulkAuditTimestamps.ApplyInsertTimestamps(mediaItems);

        // Assert
        foreach (var item in mediaItems)
        {
            Assert.NotEqual(default, item.CreatedAt);
            Assert.NotEqual(default, item.UpdatedAt);
            Assert.True(item.CreatedAt >= beforeTime);
        }
    }

    /// <summary>
    /// Verifies that an empty collection does not throw an exception.
    /// </summary>
    [Fact]
    public void ApplyInsertTimestampsHandlesEmptyCollection()
    {
        // Arrange
        var entities = new List<MetadataItem>();

        // Act & Assert - should not throw
        var exception = Record.Exception(() => BulkAuditTimestamps.ApplyInsertTimestamps(entities));
        Assert.Null(exception);
    }

    /// <summary>
    /// Verifies that ApplyUpdateTimestamps only sets UpdatedAt and leaves CreatedAt unchanged.
    /// </summary>
    [Fact]
    public void ApplyUpdateTimestampsOnlySetsUpdatedAt()
    {
        // Arrange
        var originalCreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var entities = new List<MetadataItem>
        {
            new()
            {
                Title = "Test",
                SortTitle = "Test",
                MetadataType = MetadataType.Movie,
                CreatedAt = originalCreatedAt,
            },
        };

        var beforeTime = DateTime.UtcNow;

        // Act
        BulkAuditTimestamps.ApplyUpdateTimestamps(entities);

        // Assert
        var entity = entities[0];
        Assert.Equal(originalCreatedAt, entity.CreatedAt); // CreatedAt should be unchanged
        Assert.NotEqual(default, entity.UpdatedAt);
        Assert.True(entity.UpdatedAt >= beforeTime);
        Assert.NotEqual(entity.CreatedAt, entity.UpdatedAt);
    }

    /// <summary>
    /// Verifies that ApplyUpdateTimestamps with an empty collection does not throw.
    /// </summary>
    [Fact]
    public void ApplyUpdateTimestampsHandlesEmptyCollection()
    {
        // Arrange
        var entities = new List<MetadataItem>();

        // Act & Assert - should not throw
        var exception = Record.Exception(() => BulkAuditTimestamps.ApplyUpdateTimestamps(entities));
        Assert.Null(exception);
    }
}
