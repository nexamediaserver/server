// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity type configuration for <see cref="JobNotificationEntry"/>.
/// </summary>
public class JobNotificationEntryConfiguration : IEntityTypeConfiguration<JobNotificationEntry>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<JobNotificationEntry> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.LibrarySectionId).IsRequired();
        builder.Property(e => e.JobType).IsRequired();
        builder.Property(e => e.Status).IsRequired();
        builder.Property(e => e.Progress).IsRequired();
        builder.Property(e => e.CompletedItems).IsRequired();
        builder.Property(e => e.TotalItems).IsRequired();
        builder.Property(e => e.StartedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();
        builder.Property(e => e.ErrorMessage).HasMaxLength(4096);

        builder
            .HasOne(e => e.LibrarySection)
            .WithMany()
            .HasForeignKey(e => e.LibrarySectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Composite index for efficient lookup by library and job type
        builder.HasIndex(e => new
        {
            e.LibrarySectionId,
            e.JobType,
            e.Status,
        });

        // Index for cleanup queries
        builder.HasIndex(e => e.CompletedAt);

        // Index for active job queries
        builder.HasIndex(e => e.Status);
    }
}
