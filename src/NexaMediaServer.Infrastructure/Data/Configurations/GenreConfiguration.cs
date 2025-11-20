// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity type configuration for <see cref="Genre"/>.
/// </summary>
public class GenreConfiguration : IEntityTypeConfiguration<Genre>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Genre> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Uuid).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(128);

        // Unique constraint on name
        builder.HasIndex(e => e.Name).IsUnique();

        // Self-referencing hierarchy with unlimited depth
        builder
            .HasOne(e => e.ParentGenre)
            .WithMany(e => e.ChildGenres)
            .HasForeignKey(e => e.ParentGenreId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Many-to-many relationship with MetadataItem
        builder
            .HasMany(e => e.MetadataItems)
            .WithMany(e => e.Genres)
            .UsingEntity<Dictionary<string, object>>(
                "MetadataItemGenre",
                j =>
                    j.HasOne<MetadataItem>()
                        .WithMany()
                        .HasForeignKey("MetadataItemId")
                        .OnDelete(DeleteBehavior.Cascade),
                j =>
                    j.HasOne<Genre>()
                        .WithMany()
                        .HasForeignKey("GenreId")
                        .OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("MetadataItemId", "GenreId");
                    j.HasIndex("GenreId");
                }
            );
    }
}
