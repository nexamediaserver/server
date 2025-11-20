// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity type configuration for <see cref="Tag"/>.
/// </summary>
public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Uuid).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(128);

        // Unique constraint on name
        builder.HasIndex(e => e.Name).IsUnique();

        // Many-to-many relationship with MetadataItem
        builder
            .HasMany(e => e.MetadataItems)
            .WithMany(e => e.Tags)
            .UsingEntity<Dictionary<string, object>>(
                "MetadataItemTag",
                j =>
                    j.HasOne<MetadataItem>()
                        .WithMany()
                        .HasForeignKey("MetadataItemId")
                        .OnDelete(DeleteBehavior.Cascade),
                j =>
                    j.HasOne<Tag>()
                        .WithMany()
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("MetadataItemId", "TagId");
                    j.HasIndex("TagId");
                }
            );
    }
}
