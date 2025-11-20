// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity type configuration for <see cref="LibrarySection"/>.
/// </summary>
public class LibrarySectionConfiguration : IEntityTypeConfiguration<LibrarySection>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<LibrarySection> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Uuid).IsRequired();
        builder.Property(e => e.Name).IsRequired().HasMaxLength(128);
        builder.Property(e => e.SortName).IsRequired().HasMaxLength(128);
        builder.Property(e => e.Type).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        builder
            .HasMany(e => e.Locations)
            .WithOne(e => e.LibrarySection)
            .HasForeignKey(e => e.LibrarySectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(e => e.Directories)
            .WithOne(e => e.LibrarySection)
            .HasForeignKey(e => e.LibrarySectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(e => e.MetadataItems)
            .WithOne(e => e.LibrarySection)
            .HasForeignKey(e => e.LibrarySectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.Name).IsUnique();
        builder.HasIndex(e => e.Type);
        builder.HasIndex(e => e.LastScannedAt);

        // Persist Settings as JSON in a single TEXT column for flexibility.
        builder
            .Property(e => e.Settings)
            .HasConversion(
                v =>
                    System.Text.Json.JsonSerializer.Serialize(
                        v,
                        (System.Text.Json.JsonSerializerOptions?)null
                    ),
                v =>
                    System.Text.Json.JsonSerializer.Deserialize<LibrarySectionSetting>(
                        v,
                        (System.Text.Json.JsonSerializerOptions?)null
                    )
                    ?? new LibrarySectionSetting()
            )
            .HasColumnType("TEXT")
            .IsRequired();
    }
}
