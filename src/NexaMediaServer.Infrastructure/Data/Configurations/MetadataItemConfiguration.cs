// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity type configuration for <see cref="MetadataItem"/>.
/// </summary>
public class MetadataItemConfiguration : IEntityTypeConfiguration<MetadataItem>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<MetadataItem> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Uuid).IsRequired();
        builder.Property(e => e.MetadataType).IsRequired();
        builder.Property(e => e.Title).IsRequired().HasMaxLength(256);
        builder
            .Property(e => e.SortTitle)
            .IsRequired()
            .HasMaxLength(256)
            .UseCollation("NATURALSORT");
        builder.Property(e => e.OriginalTitle).HasMaxLength(256);
        builder.Property(e => e.Tagline).HasMaxLength(256);

        builder
            .HasOne(e => e.LibrarySection)
            .WithMany(e => e.MetadataItems)
            .HasForeignKey(e => e.LibrarySectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.Parent)
            .WithMany(e => e.Children)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(e => e.MediaItems)
            .WithOne(e => e.MetadataItem)
            .HasForeignKey(e => e.MetadataItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(e => e.OutgoingRelations)
            .WithOne(r => r.MetadataItem)
            .HasForeignKey(r => r.MetadataItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(e => e.IncomingRelations)
            .WithOne(r => r.RelatedMetadataItem)
            .HasForeignKey(r => r.RelatedMetadataItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.Uuid).IsUnique();
        builder.HasIndex(e => e.MetadataType);
        builder.HasIndex(e => e.Title);
        builder.HasIndex(e => e.SortTitle);
        builder.HasIndex(e => e.OriginalTitle);
        builder.HasIndex(e => e.Index);
        builder.HasIndex(e => e.AbsoluteIndex);
        builder.HasIndex(e => e.ParentId);
        builder.HasIndex(e => e.LibrarySectionId);
    }
}
