// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity type configuration for <see cref="ExternalIdentifier"/>.
/// </summary>
public class ExternalIdentifierConfiguration : IEntityTypeConfiguration<ExternalIdentifier>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ExternalIdentifier> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Provider).IsRequired().HasMaxLength(64);
        builder.Property(e => e.Value).IsRequired().HasMaxLength(256);

        builder
            .HasOne(e => e.MetadataItem)
            .WithMany(e => e.ExternalIdentifiers)
            .HasForeignKey(e => e.MetadataItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: each metadata item can have only one identifier per provider
        builder.HasIndex(e => new { e.MetadataItemId, e.Provider }).IsUnique();

        // Index for reverse lookups: find metadata items by provider + value
        builder.HasIndex(e => new { e.Provider, e.Value });
    }
}
