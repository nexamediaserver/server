// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity type configuration for <see cref="SectionLocation"/>.
/// </summary>
public class SectionLocationConfiguration : IEntityTypeConfiguration<SectionLocation>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<SectionLocation> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RootPath).IsRequired();

        builder
            .HasOne(e => e.LibrarySection)
            .WithMany(e => e.Locations)
            .HasForeignKey(e => e.LibrarySectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(e => e.MediaItems)
            .WithOne(e => e.SectionLocation)
            .HasForeignKey(e => e.SectionLocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.RootPath, e.LibrarySectionId }).IsUnique();
        builder.HasIndex(e => e.LastScannedAt);
    }
}
