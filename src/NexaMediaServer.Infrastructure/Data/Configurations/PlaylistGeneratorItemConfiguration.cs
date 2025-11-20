// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity type configuration for <see cref="PlaylistGeneratorItem"/>.
/// </summary>
public class PlaylistGeneratorItemConfiguration : IEntityTypeConfiguration<PlaylistGeneratorItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PlaylistGeneratorItem> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.SortOrder).IsRequired();
        builder.Property(e => e.Served).HasDefaultValue(false);
        builder.Property(e => e.Cohort).HasMaxLength(128);

        builder.HasIndex(e => new { e.PlaylistGeneratorId, e.SortOrder }).IsUnique();
        builder.HasIndex(e => e.PlaylistGeneratorId);

        builder
            .HasOne(e => e.PlaylistGenerator)
            .WithMany(g => g.Items)
            .HasForeignKey(e => e.PlaylistGeneratorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.MetadataItem)
            .WithMany()
            .HasForeignKey(e => e.MetadataItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.MediaItem)
            .WithMany()
            .HasForeignKey(e => e.MediaItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasOne(e => e.MediaPart)
            .WithMany()
            .HasForeignKey(e => e.MediaPartId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
