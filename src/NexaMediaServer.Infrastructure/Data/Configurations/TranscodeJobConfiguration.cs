// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity type configuration for <see cref="TranscodeJob"/>.
/// </summary>
public class TranscodeJobConfiguration : IEntityTypeConfiguration<TranscodeJob>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TranscodeJob> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Protocol).HasMaxLength(16).HasDefaultValue("dash");
        builder.Property(e => e.State).HasConversion<int>();
        builder.Property(e => e.OutputPath).HasMaxLength(1024);
        builder.Property(e => e.ErrorMessage).HasMaxLength(2048);

        builder.HasIndex(e => e.PlaybackSessionId);
        builder.HasIndex(e => e.MediaPartId);
        builder.HasIndex(e => e.State);
        builder.HasIndex(e => e.LastPingAt);

        builder
            .HasOne(e => e.PlaybackSession)
            .WithMany()
            .HasForeignKey(e => e.PlaybackSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.MediaPart)
            .WithMany()
            .HasForeignKey(e => e.MediaPartId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
