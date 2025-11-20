// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity type configuration for <see cref="MediaPart"/>.
/// </summary>
public class MediaPartConfiguration : IEntityTypeConfiguration<MediaPart>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<MediaPart> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.MediaItemId).IsRequired();
        builder.Property(e => e.Hash).HasMaxLength(40);
        builder.Property(e => e.File).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder
            .HasOne(e => e.MediaItem)
            .WithMany(e => e.Parts)
            .HasForeignKey(e => e.MediaItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.Hash);
        builder.HasIndex(e => e.File);
        builder.HasIndex(e => e.Size);
        builder.HasIndex(e => e.MediaItemId);
        builder.HasIndex(e => e.DeletedAt);
    }
}
