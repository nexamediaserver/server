// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaMediaServer.Core.DTOs.Playback;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity type configuration for <see cref="CapabilityProfile"/>.
/// </summary>
public class CapabilityProfileConfiguration : IEntityTypeConfiguration<CapabilityProfile>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CapabilityProfile> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Version).IsRequired().HasDefaultValue(1);
        builder.Property(e => e.DeviceId).HasMaxLength(256);
        builder.Property(e => e.Name).HasMaxLength(256);
        builder
            .Property(e => e.Capabilities)
            .HasColumnName("CapabilitiesJson")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v =>
                    JsonSerializer.Deserialize<PlaybackCapabilities>(
                        v,
                        (JsonSerializerOptions?)null
                    ) ?? new PlaybackCapabilities()
            )
            .IsRequired();
        builder.Property(e => e.DeclaredAt).IsRequired();

        builder.HasIndex(e => new { e.SessionId, e.Version }).IsUnique();
        builder.HasIndex(e => e.SessionId);

        builder
            .HasOne(e => e.Session)
            .WithMany()
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
