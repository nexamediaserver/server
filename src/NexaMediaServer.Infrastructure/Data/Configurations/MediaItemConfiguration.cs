// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity type configuration for <see cref="MediaItem"/>.
/// </summary>
public class MediaItemConfiguration : IEntityTypeConfiguration<MediaItem>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<MediaItem> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.MetadataItemId).IsRequired();
        builder.Property(e => e.SectionLocationId).IsRequired();

        builder
            .HasOne(e => e.MetadataItem)
            .WithMany(e => e.MediaItems)
            .HasForeignKey(e => e.MetadataItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.SectionLocation)
            .WithMany(e => e.MediaItems)
            .HasForeignKey(e => e.SectionLocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(e => e.Parts)
            .WithOne(e => e.MediaItem)
            .HasForeignKey(e => e.MediaItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.MetadataItemId);
        builder.HasIndex(e => e.SectionLocationId);
    }
}
