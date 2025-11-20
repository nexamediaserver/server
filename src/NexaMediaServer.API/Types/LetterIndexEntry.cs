// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.API.Types;

/// <summary>
/// Represents a letter in the alphabetical index with its item count and position.
/// Used for jump bar navigation in library browse views.
/// </summary>
[GraphQLName("LetterIndexEntry")]
public sealed class LetterIndexEntry
{
    /// <summary>
    /// Gets the letter for this index entry.
    /// "#" represents all non-alphabetic characters (numbers, symbols).
    /// "A" through "Z" represent alphabetic characters.
    /// </summary>
    public string Letter { get; init; } = null!;

    /// <summary>
    /// Gets the number of items starting with this letter.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Gets the zero-based offset of the first item starting with this letter
    /// in the sorted list. Used for skip-based pagination jumps.
    /// </summary>
    public int FirstItemOffset { get; init; }
}
