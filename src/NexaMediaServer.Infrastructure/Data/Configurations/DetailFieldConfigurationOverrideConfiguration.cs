// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity type configuration for <see cref="DetailFieldConfigurationOverride"/>.
/// </summary>
public sealed class DetailFieldConfigurationOverrideConfiguration
    : IEntityTypeConfiguration<DetailFieldConfigurationOverride>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DetailFieldConfigurationOverride> builder)
    {
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.MetadataType).IsRequired();

        builder
            .Property(entity => entity.EnabledFieldTypes)
            .HasConversion(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value =>
                    JsonSerializer.Deserialize<List<DetailFieldType>>(
                        value,
                        (JsonSerializerOptions?)null
                    ) ?? new List<DetailFieldType>()
            )
            .HasColumnType("TEXT")
            .HasDefaultValue(new List<DetailFieldType>())
            .IsRequired();

        builder
            .Property(entity => entity.DisabledFieldTypes)
            .HasConversion(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value =>
                    JsonSerializer.Deserialize<List<DetailFieldType>>(
                        value,
                        (JsonSerializerOptions?)null
                    ) ?? new List<DetailFieldType>()
            )
            .HasColumnType("TEXT")
            .HasDefaultValue(new List<DetailFieldType>())
            .IsRequired();

        builder
            .Property(entity => entity.DisabledCustomFieldKeys)
            .HasConversion(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value =>
                    JsonSerializer.Deserialize<List<string>>(value, (JsonSerializerOptions?)null)
                    ?? new List<string>()
            )
            .HasColumnType("TEXT")
            .HasDefaultValue(new List<string>())
            .IsRequired();

        builder
            .HasOne(entity => entity.LibrarySection)
            .WithMany()
            .HasForeignKey(entity => entity.LibrarySectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(entity => new { entity.MetadataType, entity.LibrarySectionId })
            .IsUnique();

        builder.HasIndex(entity => entity.MetadataType);
    }
}
