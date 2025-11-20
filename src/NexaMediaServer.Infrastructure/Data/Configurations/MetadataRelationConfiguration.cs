// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity type configuration for <see cref="MetadataRelation"/>.
/// </summary>
public sealed class MetadataRelationConfiguration : IEntityTypeConfiguration<MetadataRelation>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<MetadataRelation> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Text).UseCollation("NOCASE");

        builder.Property(r => r.RelationType).IsRequired();

        builder
            .HasOne(r => r.MetadataItem)
            .WithMany(m => m.OutgoingRelations)
            .HasForeignKey(r => r.MetadataItemId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne(r => r.RelatedMetadataItem)
            .WithMany(m => m.IncomingRelations)
            .HasForeignKey(r => r.RelatedMetadataItemId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasIndex(r => new
            {
                r.MetadataItemId,
                r.RelatedMetadataItemId,
                r.RelationType,
                r.Text,
            })
            .IsUnique();
    }
}
