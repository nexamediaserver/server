// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity type configuration for <see cref="MetadataItemSetting"/>.
/// </summary>
public class MetadataItemSettingConfiguration : IEntityTypeConfiguration<MetadataItemSetting>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<MetadataItemSetting> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserId).IsRequired().HasMaxLength(450);
        builder.Property(e => e.MetadataItemId).IsRequired();
        builder.Property(e => e.Rating).HasDefaultValue(0);
        builder.Property(e => e.ViewOffset).HasDefaultValue(0);
        builder.Property(e => e.ViewCount).HasDefaultValue(0);
        builder.Property(e => e.SkipCount).HasDefaultValue(0);

        builder
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.MetadataItem)
            .WithMany(e => e.Settings)
            .HasForeignKey(e => e.MetadataItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ensure that each user can only have one setting per metadata item
        builder.HasIndex(e => new { e.UserId, e.MetadataItemId }).IsUnique();

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.MetadataItemId);
        builder.HasIndex(e => e.LastViewedAt);
        builder.HasIndex(e => e.LastSkippedAt);
    }
}
