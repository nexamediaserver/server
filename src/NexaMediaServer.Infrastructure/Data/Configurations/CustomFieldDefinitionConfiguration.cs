// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity type configuration for <see cref="CustomFieldDefinition"/>.
/// </summary>
public class CustomFieldDefinitionConfiguration : IEntityTypeConfiguration<CustomFieldDefinition>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<CustomFieldDefinition> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Key).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Label).IsRequired().HasMaxLength(256);
        builder.Property(e => e.Widget).IsRequired();
        builder.Property(e => e.SortOrder).IsRequired();
        builder.Property(e => e.IsEnabled).IsRequired().HasDefaultValue(true);

        // Store applicable metadata types as JSON
        builder
            .Property(e => e.ApplicableMetadataTypes)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v =>
                    JsonSerializer.Deserialize<List<MetadataType>>(
                        v,
                        (JsonSerializerOptions?)null
                    ) ?? new List<MetadataType>()
            )
            .HasColumnType("TEXT")
            .HasDefaultValue(new List<MetadataType>())
            .IsRequired();

        // Ensure unique key
        builder.HasIndex(e => e.Key).IsUnique();

        // Index for enabled fields
        builder.HasIndex(e => e.IsEnabled);

        // Index for sort order
        builder.HasIndex(e => e.SortOrder);
    }
}
