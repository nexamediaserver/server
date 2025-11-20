// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity type configuration for <see cref="PlaylistGenerator"/>.
/// </summary>
public class PlaylistGeneratorConfiguration : IEntityTypeConfiguration<PlaylistGenerator>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PlaylistGenerator> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.PlaylistGeneratorId).IsRequired();
        builder.Property(e => e.SeedJson).IsRequired();
        builder.Property(e => e.Cursor).IsRequired();
        builder.Property(e => e.Repeat).IsRequired();
        builder.Property(e => e.Shuffle).IsRequired();
        builder.Property(e => e.ShuffleState).HasMaxLength(512);
        builder.Property(e => e.ExpiresAt).IsRequired();
        builder.Property(e => e.ChunkSize).HasDefaultValue(PlaybackDefaults.PlaylistChunkSize);

        builder.HasIndex(e => e.PlaybackSessionId).IsUnique();
        builder.HasIndex(e => e.PlaylistGeneratorId).IsUnique();
        builder.HasIndex(e => e.ExpiresAt);

        builder
            .HasOne(e => e.PlaybackSession)
            .WithOne(s => s.PlaylistGenerator)
            .HasForeignKey<PlaylistGenerator>(e => e.PlaybackSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
