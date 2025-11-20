// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Infrastructure.Data.Configurations;

/// <summary>
/// Entity type configuration for <see cref="Core.Entities.Directory"/>.
/// </summary>
public class DirectoryConfiguration : IEntityTypeConfiguration<Core.Entities.Directory>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Core.Entities.Directory> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.LibrarySectionId).IsRequired();
        builder.Property(e => e.Path).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder
            .HasOne(e => e.ParentDirectory)
            .WithMany(e => e.SubDirectories)
            .HasForeignKey(e => e.ParentDirectoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(e => e.LibrarySection)
            .WithMany(e => e.Directories)
            .HasForeignKey(e => e.LibrarySectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.ParentDirectoryId);
        builder.HasIndex(e => e.Path);
        builder
            .HasIndex(e => new
            {
                e.LibrarySectionId,
                e.ParentDirectoryId,
                e.Path,
            })
            .IsUnique();
        builder.HasIndex(e => e.DeletedAt);
    }
}
