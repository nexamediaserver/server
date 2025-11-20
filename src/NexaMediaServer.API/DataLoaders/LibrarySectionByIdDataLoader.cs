// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using NexaMediaServer.API.Types;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.API.DataLoaders;

/// <summary>
/// Data loader for fetching library sections by their UUID identifiers.
/// </summary>
public sealed class LibrarySectionByIdDataLoader
    : DataLoaderBase<Guid, LibrarySection>,
        ILibrarySectionByIdDataLoader
{
    private readonly ILibrarySectionService librarySectionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibrarySectionByIdDataLoader"/> class.
    /// </summary>
    /// <param name="librarySectionService">The library section service.</param>
    /// <param name="batchScheduler">The batch scheduler.</param>
    /// <param name="options">Data loader options supplied by DI.</param>
    public LibrarySectionByIdDataLoader(
        ILibrarySectionService librarySectionService,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options
    )
        : base(batchScheduler, options ?? throw new ArgumentNullException(nameof(options)))
    {
        this.librarySectionService =
            librarySectionService ?? throw new ArgumentNullException(nameof(librarySectionService));
    }

    /// <inheritdoc />
    protected override async ValueTask FetchAsync(
        IReadOnlyList<Guid> keys,
        Memory<Result<LibrarySection?>> results,
        DataLoaderFetchContext<LibrarySection> context,
        CancellationToken cancellationToken
    )
    {
        _ = context;
        if (keys.Count == 0)
        {
            return;
        }

        var distinctKeys = keys.Distinct().ToArray();

        var sections = await this
            .librarySectionService.GetQueryable()
            .Where(section => distinctKeys.Contains(section.Uuid))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var mappedById = sections
            .Select(LibrarySection.FromEntity)
            .ToDictionary(section => section.Id, section => section);

        var span = results.Span;
        for (var i = 0; i < keys.Count; i++)
        {
            span[i] = mappedById.TryGetValue(keys[i], out var section)
                ? Result<LibrarySection?>.Resolve(section)
                : Result<LibrarySection?>.Resolve(null);
        }
    }
}
