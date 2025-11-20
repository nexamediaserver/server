// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NexaMediaServer.Infrastructure.Data;

/// <summary>
/// Design-time context factory for EF Core migrations.
/// </summary>
internal sealed class DesignTimeMediaServerContextFactory
    : IDesignTimeDbContextFactory<MediaServerContext>
{
    /// <inheritdoc />
    public MediaServerContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MediaServerContext>();
        optionsBuilder.UseSqlite("Data Source=design_time.db");

        return new MediaServerContext(optionsBuilder.Options);
    }
}
