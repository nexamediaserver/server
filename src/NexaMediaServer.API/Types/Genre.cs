// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Represents a genre for GraphQL API.
/// </summary>
[Node]
[GraphQLName("Genre")]
public class Genre
{
    /// <summary>
    /// Gets the unique identifier for the genre.
    /// </summary>
    [ID]
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the genre name.
    /// </summary>
    public string Name { get; init; } = null!;

    /// <summary>
    /// Gets the parent genre identifier.
    /// </summary>
    [ID("Genre")]
    public Guid? ParentId { get; init; }

    /// <summary>
    /// Gets the child genres of this genre.
    /// </summary>
    /// <param name="contextFactory">The database context factory.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of child genres.</returns>
    [GraphQLName("children")]
    public async Task<List<Genre>> GetChildrenAsync(
        [Service] IDbContextFactory<MediaServerContext> contextFactory,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var genreId = await context
            .Genres.Where(g => g.Uuid == this.Id)
            .Select(g => g.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (genreId == 0)
        {
            return [];
        }

        return await context
            .Genres.Where(g => g.ParentGenreId == genreId)
            .Select(g => new Genre
            {
                Id = g.Uuid,
                Name = g.Name,
                ParentId = this.Id,
            })
            .ToListAsync(cancellationToken);
    }
}
