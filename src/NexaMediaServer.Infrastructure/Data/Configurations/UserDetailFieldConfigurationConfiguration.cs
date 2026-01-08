// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity type configuration for <see cref="UserDetailFieldConfiguration"/>.
/// </summary>
public class UserDetailFieldConfigurationConfiguration
    : IEntityTypeConfiguration<UserDetailFieldConfiguration>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<UserDetailFieldConfiguration> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserId).IsRequired().HasMaxLength(450);
        builder.Property(e => e.MetadataType).IsRequired();

        // Store field type lists as JSON
        builder
            .Property(e => e.EnabledFieldTypes)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v =>
                    JsonSerializer.Deserialize<List<DetailFieldType>>(
                        v,
                        (JsonSerializerOptions?)null
                    ) ?? new List<DetailFieldType>()
            )
            .HasColumnType("TEXT")
            .HasDefaultValue(new List<DetailFieldType>())
            .IsRequired();

        builder
            .Property(e => e.DisabledFieldTypes)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v =>
                    JsonSerializer.Deserialize<List<DetailFieldType>>(
                        v,
                        (JsonSerializerOptions?)null
                    ) ?? new List<DetailFieldType>()
            )
            .HasColumnType("TEXT")
            .HasDefaultValue(new List<DetailFieldType>())
            .IsRequired();

        builder
            .Property(e => e.DisabledCustomFieldKeys)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v =>
                    JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)
                    ?? new List<string>()
            )
            .HasColumnType("TEXT")
            .HasDefaultValue(new List<string>())
            .IsRequired();

        builder
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ensure unique configuration per user+metadata type combination
        builder.HasIndex(e => new { e.UserId, e.MetadataType }).IsUnique();

        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.MetadataType);
    }
}
