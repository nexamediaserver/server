// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NaturalSort.Extension;

namespace NexaMediaServer.Infrastructure.Data.Interceptors;

/// <summary>
/// Interceptor that registers a custom natural sort collation with SQLite connections.
/// </summary>
public class SqliteNaturalSortInterceptor : DbConnectionInterceptor
{
    private static readonly NaturalSortComparer NaturalComparer = new(
        StringComparison.OrdinalIgnoreCase
    );

    /// <inheritdoc/>
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        base.ConnectionOpened(connection, eventData);
        RegisterCollation(connection);
    }

    /// <inheritdoc/>
    public override Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default
    )
    {
        RegisterCollation(connection);
        return base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private static void RegisterCollation(DbConnection connection)
    {
        if (connection is SqliteConnection sqliteConnection)
        {
            sqliteConnection.CreateCollation(
                "NATURALSORT",
                (x, y) => NaturalComparer.Compare(x, y)
            );
        }
    }
}
