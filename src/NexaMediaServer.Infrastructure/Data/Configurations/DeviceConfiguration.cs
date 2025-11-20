// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for <see cref="Device"/>.
/// </summary>
public sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.HasKey(device => device.Id);

        builder.Property(device => device.Identifier).IsRequired().HasMaxLength(256);
        builder.Property(device => device.Name).IsRequired().HasMaxLength(256);
        builder.Property(device => device.Platform).IsRequired().HasMaxLength(128);
        builder.Property(device => device.Version).HasMaxLength(64);
        builder.Property(device => device.LastSeenIp).HasMaxLength(64);

        builder
            .HasOne(device => device.User)
            .WithMany(user => user.Devices)
            .HasForeignKey(device => device.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(device => device.Sessions)
            .WithOne(session => session.Device)
            .HasForeignKey(session => session.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(device => new { device.UserId, device.Identifier }).IsUnique();
        builder.HasIndex(device => device.LastSeenIp);
    }
}
