// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using HotChocolate.Data.Sorting;

namespace NexaMediaServer.API.Types;

/// <summary>
/// Defines sorting options for <see cref="MetadataItem"/>.
/// </summary>
public class MetadataItemSortType : SortInputType<MetadataItem>
{
    /// <inheritdoc/>
    protected override void Configure(ISortInputTypeDescriptor<MetadataItem> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        // Natural sorting with case-insensitive comparison is handled by the NATURALSORT collation in SQLite
        descriptor.Field(t => t.TitleSort).Name("title");
        descriptor.Field(t => t.Year).Name("year");
    }
}
