// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for <see cref="Session"/>.
/// </summary>
public sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasKey(session => session.Id);

        builder.Property(session => session.PublicId).IsRequired();
        builder.Property(session => session.UserId).IsRequired();
        builder.Property(session => session.DeviceId).IsRequired();
        builder.Property(session => session.ExpiresAt).IsRequired();
        builder.Property(session => session.CreatedFromIp).HasMaxLength(64);
        builder.Property(session => session.ClientVersion).HasMaxLength(64);
        builder.Property(session => session.RevokedReason).HasMaxLength(256);

        builder
            .HasOne(session => session.User)
            .WithMany(user => user.Sessions)
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(session => session.Device)
            .WithMany(device => device.Sessions)
            .HasForeignKey(session => session.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(session => session.PublicId).IsUnique();
        builder.HasIndex(session => session.UserId);
        builder.HasIndex(session => session.DeviceId);
        builder.HasIndex(session => session.ExpiresAt);
        builder.HasIndex(session => session.RevokedAt);
    }
}
