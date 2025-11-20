// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity type configuration for <see cref="LibraryScan"/>.
/// </summary>
public class LibraryScanConfiguration : IEntityTypeConfiguration<LibraryScan>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<LibraryScan> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.LibrarySectionId).IsRequired();
        builder.Property(e => e.Status).IsRequired();
        builder.Property(e => e.StartedAt).IsRequired();

        builder
            .HasOne(e => e.LibrarySection)
            .WithMany(e => e.Scans)
            .HasForeignKey(e => e.LibrarySectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.LibrarySectionId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.StartedAt);
    }
}
