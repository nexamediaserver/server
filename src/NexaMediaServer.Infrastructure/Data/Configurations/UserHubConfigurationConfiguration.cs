// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity type configuration for <see cref="UserHubConfiguration"/>.
/// </summary>
public class UserHubConfigurationConfiguration : IEntityTypeConfiguration<UserHubConfiguration>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<UserHubConfiguration> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserId).IsRequired().HasMaxLength(450);
        builder.Property(e => e.Context).IsRequired();

        // Store hub type lists as JSON
        builder
            .Property(e => e.EnabledHubTypes)
            .HasConversion(
                v =>
                    System.Text.Json.JsonSerializer.Serialize(
                        v,
                        (System.Text.Json.JsonSerializerOptions?)null
                    ),
                v =>
                    System.Text.Json.JsonSerializer.Deserialize<List<HubType>>(
                        v,
                        (System.Text.Json.JsonSerializerOptions?)null
                    ) ?? new List<HubType>()
            );

        builder
            .Property(e => e.DisabledHubTypes)
            .HasConversion(
                v =>
                    System.Text.Json.JsonSerializer.Serialize(
                        v,
                        (System.Text.Json.JsonSerializerOptions?)null
                    ),
                v =>
                    System.Text.Json.JsonSerializer.Deserialize<List<HubType>>(
                        v,
                        (System.Text.Json.JsonSerializerOptions?)null
                    ) ?? new List<HubType>()
            );

        builder
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(e => e.LibrarySection)
            .WithMany()
            .HasForeignKey(e => e.LibrarySectionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ensure unique configuration per user+context+library combination
        builder
            .HasIndex(e => new
            {
                e.UserId,
                e.Context,
                e.LibrarySectionId,
            })
            .IsUnique();

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.Context);
    }
}
