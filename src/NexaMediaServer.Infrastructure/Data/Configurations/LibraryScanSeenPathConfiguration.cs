// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity type configuration for <see cref="LibraryScanSeenPath"/>.
/// </summary>
public class LibraryScanSeenPathConfiguration : IEntityTypeConfiguration<LibraryScanSeenPath>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<LibraryScanSeenPath> builder)
    {
        // Composite primary key: (LibraryScanId, FilePath)
        builder.HasKey(e => new { e.LibraryScanId, e.FilePath });

        builder.Property(e => e.FilePath).IsRequired().HasMaxLength(4096);
        builder.Property(e => e.SeenAt).IsRequired();

        builder
            .HasOne(e => e.LibraryScan)
            .WithMany(e => e.SeenPaths)
            .HasForeignKey(e => e.LibraryScanId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index on LibraryScanId for efficient queries
        builder.HasIndex(e => e.LibraryScanId);
    }
}
