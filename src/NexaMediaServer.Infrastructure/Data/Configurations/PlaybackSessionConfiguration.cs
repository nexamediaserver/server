// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity type configuration for <see cref="PlaybackSession"/>.
/// </summary>
public class PlaybackSessionConfiguration : IEntityTypeConfiguration<PlaybackSession>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PlaybackSession> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.PlaybackSessionId).IsRequired();
        builder.Property(e => e.Originator).HasMaxLength(256);
        builder.Property(e => e.State).HasMaxLength(32).HasDefaultValue("playing");
        builder.Property(e => e.LastHeartbeatAt).IsRequired();
        builder.Property(e => e.ExpiresAt).IsRequired();

        builder.HasIndex(e => e.PlaybackSessionId).IsUnique();
        builder.HasIndex(e => e.SessionId);
        builder.HasIndex(e => e.CapabilityProfileId);
        builder.HasIndex(e => e.ExpiresAt);

        builder
            .HasOne(e => e.Session)
            .WithMany()
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.CapabilityProfile)
            .WithMany()
            .HasForeignKey(e => e.CapabilityProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(e => e.CurrentMetadataItem)
            .WithMany()
            .HasForeignKey(e => e.CurrentMetadataItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(e => e.CurrentMediaPart)
            .WithMany()
            .HasForeignKey(e => e.CurrentMediaPartId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
