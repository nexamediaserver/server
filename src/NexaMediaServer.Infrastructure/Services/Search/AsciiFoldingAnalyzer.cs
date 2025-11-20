// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;

namespace NexaMediaServer.Infrastructure.Services.Search;

/// <summary>
/// A custom analyzer that normalizes Unicode characters to ASCII equivalents.
/// </summary>
public sealed class AsciiFoldingAnalyzer : Analyzer
{
    private readonly LuceneVersion matchVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsciiFoldingAnalyzer"/> class.
    /// </summary>
    /// <param name="matchVersion">The Lucene version to match.</param>
    public AsciiFoldingAnalyzer(LuceneVersion matchVersion)
    {
        this.matchVersion = matchVersion;
    }

    /// <inheritdoc/>
    protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
    {
        // Standard tokenizer handles word boundaries and punctuation
        var tokenizer = new StandardTokenizer(this.matchVersion, reader);

        TokenStream tokenStream = tokenizer;

        // Lowercase all tokens for case-insensitive matching
        tokenStream = new LowerCaseFilter(this.matchVersion, tokenStream);

        // Convert Unicode characters to ASCII equivalents (ū → u, é → e, etc.)
        tokenStream = new ASCIIFoldingFilter(tokenStream);

        return new TokenStreamComponents(tokenizer, tokenStream);
    }
}
