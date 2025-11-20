// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Text.RegularExpressions;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Microsoft.Extensions.Logging;
using NexaMediaServer.Core.DTOs;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.Search;

/// <summary>
/// Provides full-text search capabilities using Lucene.NET.
/// </summary>
public sealed partial class LuceneSearchService : ISearchService, IAsyncDisposable
{
    /// <summary>
    /// The current schema version. Increment this when the index schema changes.
    /// </summary>
    private const int SchemaVersion = 5;

    private const string SchemaVersionFileName = ".schema_version";
    private const LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;

    // Field names
    private const string FieldUuid = "uuid";
    private const string FieldTitle = "title";
    private const string FieldOriginalTitle = "original_title";
    private const string FieldSummary = "summary";
    private const string FieldTagline = "tagline";
    private const string FieldMetadataType = "metadata_type";
    private const string FieldYear = "year";
    private const string FieldThumbUri = "thumb_uri";
    private const string FieldLibrarySectionId = "library_section_id";
    private const string FieldCast = "cast";
    private const string FieldCrew = "crew";
    private const string FieldDirectors = "directors";
    private const string FieldGenres = "genres";
    private const string FieldTags = "tags";

    private static readonly string[] SearchFields =
    [
        FieldTitle,
        FieldOriginalTitle,
        FieldSummary,
        FieldTagline,
        FieldCast,
        FieldCrew,
        FieldDirectors,
        FieldGenres,
        FieldTags,
    ];

    private readonly IApplicationPaths applicationPaths;
    private readonly SemaphoreSlim writeLock = new(1, 1);

    private FSDirectory? directory;
    private IndexWriter? indexWriter;
    private SearcherManager? searcherManager;
    private AsciiFoldingAnalyzer? analyzer;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="LuceneSearchService"/> class.
    /// </summary>
    /// <param name="applicationPaths">The application paths service.</param>
    /// <param name="logger">The logger.</param>
    public LuceneSearchService(
        IApplicationPaths applicationPaths,
        ILogger<LuceneSearchService> logger
    )
    {
        this.applicationPaths = applicationPaths;
        _ = logger; // Reserved for future use
    }

    /// <inheritdoc />
    public int ExpectedSchemaVersion => SchemaVersion;

    /// <inheritdoc />
    public int? GetIndexSchemaVersion()
    {
        var versionPath = Path.Combine(this.applicationPaths.IndexDirectory, SchemaVersionFileName);
        if (!File.Exists(versionPath))
        {
            return null;
        }

        var content = File.ReadAllText(versionPath).Trim();
        return int.TryParse(content, out var version) ? version : null;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string query,
        SearchPivot pivot = SearchPivot.Top,
        int limit = 25,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        await this.EnsureInitializedAsync(cancellationToken);

        var searcher = this.searcherManager!.Acquire();
        try
        {
            return this.ExecuteSearch(searcher, query, pivot, limit);
        }
        finally
        {
            this.searcherManager.Release(searcher);
        }
    }

    /// <inheritdoc />
    public async Task IndexItemsAsync(
        IEnumerable<MetadataItem> items,
        CancellationToken cancellationToken = default
    )
    {
        await this.EnsureInitializedAsync(cancellationToken);
        await this.writeLock.WaitAsync(cancellationToken);

        try
        {
            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Delete existing document if any
                this.indexWriter!.DeleteDocuments(new Term(FieldUuid, item.Uuid.ToString()));

                // Create and add new document
                var doc = CreateDocument(item);
                this.indexWriter.AddDocument(doc);
            }

            this.indexWriter!.Commit();
            this.searcherManager!.MaybeRefresh();
        }
        finally
        {
            this.writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task RemoveFromIndexAsync(Guid uuid, CancellationToken cancellationToken = default)
    {
        await this.EnsureInitializedAsync(cancellationToken);
        await this.writeLock.WaitAsync(cancellationToken);

        try
        {
            this.indexWriter!.DeleteDocuments(new Term(FieldUuid, uuid.ToString()));
            this.indexWriter.Commit();
            this.searcherManager!.MaybeRefresh();
        }
        finally
        {
            this.writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task RebuildIndexAsync(CancellationToken cancellationToken = default)
    {
        await this.writeLock.WaitAsync(cancellationToken);

        try
        {
            // Close existing resources
            await this.DisposeResourcesAsync();

            // Delete existing index
            var indexPath = this.applicationPaths.IndexDirectory;
            if (System.IO.Directory.Exists(indexPath))
            {
                System.IO.Directory.Delete(indexPath, recursive: true);
            }

            this.applicationPaths.EnsureDirectoryExists(indexPath);

            // Reinitialize with fresh index
            await this.InitializeAsync(cancellationToken);
        }
        finally
        {
            this.writeLock.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (this.disposed)
        {
            return;
        }

        await this.DisposeResourcesAsync();
        this.writeLock.Dispose();
        this.disposed = true;
    }

    /// <summary>
    /// Writes the schema version to the index directory.
    /// </summary>
    internal void WriteSchemaVersion()
    {
        var versionPath = Path.Combine(this.applicationPaths.IndexDirectory, SchemaVersionFileName);
        File.WriteAllText(versionPath, SchemaVersion.ToString(CultureInfo.InvariantCulture));
    }

    private static Query? CreatePivotFilter(SearchPivot pivot)
    {
        var types = pivot switch
        {
            SearchPivot.Movie => [MetadataType.Movie],
            SearchPivot.Show => [MetadataType.Show],
            SearchPivot.Episode => [MetadataType.Episode],
            SearchPivot.People => [MetadataType.Person, MetadataType.Group],
            SearchPivot.Album => [MetadataType.AlbumRelease, MetadataType.AlbumReleaseGroup],
            SearchPivot.Track => [MetadataType.Track],
            _ => Array.Empty<MetadataType>(),
        };

        if (types.Length == 0)
        {
            return null;
        }

        if (types.Length == 1)
        {
            return new TermQuery(new Term(FieldMetadataType, types[0].ToString()));
        }

        var boolQuery = new BooleanQuery();
        foreach (var type in types)
        {
            boolQuery.Add(
                new TermQuery(new Term(FieldMetadataType, type.ToString())),
                Occur.SHOULD
            );
        }

        return boolQuery;
    }

    /// <summary>
    /// Transforms a query string to make each term fuzzy by appending ~.
    /// </summary>
    private static string MakeFuzzyQuery(string query)
    {
        // Match word tokens (alphanumeric sequences)
        return Regex.Replace(query, @"(\w+)", "$1~");
    }

    private static Document CreateDocument(MetadataItem item)
    {
        var doc = new Document
        {
            new StringField(FieldUuid, item.Uuid.ToString(), Field.Store.YES),
            new TextField(FieldTitle, item.Title, Field.Store.YES),
            new StringField(FieldMetadataType, item.MetadataType.ToString(), Field.Store.YES),
        };

        AddOptionalTextField(doc, FieldOriginalTitle, item.OriginalTitle);
        AddOptionalTextField(doc, FieldSummary, item.Summary);
        AddOptionalTextField(doc, FieldTagline, item.Tagline);

        if (item.Year.HasValue)
        {
            doc.Add(
                new StringField(
                    FieldYear,
                    item.Year.Value.ToString(CultureInfo.InvariantCulture),
                    Field.Store.YES
                )
            );
        }

        if (!string.IsNullOrEmpty(item.ThumbUri))
        {
            doc.Add(new StringField(FieldThumbUri, item.ThumbUri, Field.Store.YES));
        }

        doc.Add(
            new StringField(
                FieldLibrarySectionId,
                item.LibrarySection.Uuid.ToString(),
                Field.Store.YES
            )
        );

        // Denormalize relationships
        AddRelationshipFields(doc, item);

        // Denormalize genres and tags
        AddGenresAndTagsFields(doc, item);

        return doc;
    }

    private static void AddOptionalTextField(Document doc, string fieldName, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            doc.Add(new TextField(fieldName, value, Field.Store.NO));
        }
    }

    private static void AddRelationshipFields(Document doc, MetadataItem item)
    {
        var castNames = new List<string>();
        var crewNames = new List<string>();
        var directorNames = new List<string>();

        ExtractRelationsFromOutgoing(item.OutgoingRelations, castNames, crewNames, directorNames);
        ExtractRelationsFromIncoming(item.IncomingRelations, castNames, crewNames, directorNames);

        AddListAsTextField(doc, FieldCast, castNames);
        AddListAsTextField(doc, FieldCrew, crewNames);
        AddListAsTextField(doc, FieldDirectors, directorNames);
    }

    private static void ExtractRelationsFromOutgoing(
        ICollection<MetadataRelation> relations,
        List<string> castNames,
        List<string> crewNames,
        List<string> directorNames
    )
    {
        foreach (var relation in relations)
        {
            var relatedName = relation.RelatedMetadataItem?.Title;
            if (string.IsNullOrEmpty(relatedName))
            {
                continue;
            }

            switch (relation.RelationType)
            {
                case RelationType.PersonPerformsInVideo:
                    castNames.Add(relatedName);
                    break;
                case RelationType.PersonContributesCrewToVideo:
                    crewNames.Add(relatedName);
                    if (IsDirectorRole(relation.Text))
                    {
                        directorNames.Add(relatedName);
                    }

                    break;
            }
        }
    }

    private static void ExtractRelationsFromIncoming(
        ICollection<MetadataRelation> relations,
        List<string> castNames,
        List<string> crewNames,
        List<string> directorNames
    )
    {
        foreach (var relation in relations)
        {
            var relatedName = relation.MetadataItem?.Title;
            if (string.IsNullOrEmpty(relatedName))
            {
                continue;
            }

            switch (relation.RelationType)
            {
                case RelationType.PersonPerformsInVideo:
                    castNames.Add(relatedName);
                    break;
                case RelationType.PersonContributesCrewToVideo:
                    crewNames.Add(relatedName);
                    if (IsDirectorRole(relation.Text))
                    {
                        directorNames.Add(relatedName);
                    }

                    break;
            }
        }
    }

    private static bool IsDirectorRole(string? roleText)
    {
        return !string.IsNullOrEmpty(roleText)
            && roleText.Contains("Director", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddGenresAndTagsFields(Document doc, MetadataItem item)
    {
        // Extract genre names
        var genreNames = item
            .Genres.Select(g => g.Name)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();

        // Extract tag names
        var tagNames = item.Tags.Select(t => t.Name).Where(n => !string.IsNullOrEmpty(n)).ToList();

        AddListAsTextField(doc, FieldGenres, genreNames);
        AddListAsTextField(doc, FieldTags, tagNames);
    }

    private static void AddListAsTextField(Document doc, string fieldName, List<string> values)
    {
        if (values.Count > 0)
        {
            doc.Add(new TextField(fieldName, string.Join(" ", values), Field.Store.NO));
        }
    }

#pragma warning disable CA1305 // Lucene.Net Document.Get() doesn't have IFormatProvider overload
    private static SearchResult? MapDocumentToSearchResult(
        IndexSearcher searcher,
        ScoreDoc scoreDoc
    )
    {
        var doc = searcher.Doc(scoreDoc.Doc);
        var uuidStr = doc.Get(FieldUuid);
        var title = doc.Get(FieldTitle);
        var metadataTypeStr = doc.Get(FieldMetadataType);
        var yearStr = doc.Get(FieldYear);
        var thumbUri = doc.Get(FieldThumbUri);
        var librarySectionIdStr = doc.Get(FieldLibrarySectionId);
#pragma warning restore CA1305

        if (
            !Guid.TryParse(uuidStr, out var uuid)
            || !Enum.TryParse<MetadataType>(metadataTypeStr, out var metadataType)
            || !Guid.TryParse(librarySectionIdStr, out var librarySectionId)
        )
        {
            return null;
        }

        int? year = null;
        if (
            !string.IsNullOrEmpty(yearStr)
            && int.TryParse(
                yearStr,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var parsedYear
            )
        )
        {
            year = parsedYear;
        }

        return new SearchResult(
            uuid,
            title,
            metadataType,
            scoreDoc.Score,
            year,
            thumbUri,
            librarySectionId
        );
    }

    private List<SearchResult> ExecuteSearch(
        IndexSearcher searcher,
        string query,
        SearchPivot pivot,
        int limit
    )
    {
        var parser = new MultiFieldQueryParser(AppLuceneVersion, SearchFields, this.analyzer)
        {
            DefaultOperator = Operator.AND,
        };

        Query luceneQuery;
        try
        {
            // Escape special characters and make each term fuzzy by appending ~
            var escapedQuery = QueryParserBase.Escape(query);
            var fuzzyQuery = MakeFuzzyQuery(escapedQuery);
            luceneQuery = parser.Parse(fuzzyQuery);
        }
        catch (ParseException)
        {
            return [];
        }

        // Apply pivot filter if not Top
        if (pivot != SearchPivot.Top)
        {
            var typeFilter = CreatePivotFilter(pivot);
            if (typeFilter != null)
            {
                var booleanQuery = new BooleanQuery
                {
                    { luceneQuery, Occur.MUST },
                    { typeFilter, Occur.MUST },
                };
                luceneQuery = booleanQuery;
            }
        }

        var topDocs = searcher.Search(luceneQuery, limit);
        var results = new List<SearchResult>(topDocs.ScoreDocs.Length);

        foreach (var scoreDoc in topDocs.ScoreDocs)
        {
            var result = MapDocumentToSearchResult(searcher, scoreDoc);
            if (result != null)
            {
                results.Add(result);
            }
        }

        return results;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (this.indexWriter != null && this.searcherManager != null)
        {
            return;
        }

        await this.writeLock.WaitAsync(cancellationToken);
        try
        {
            if (this.indexWriter == null || this.searcherManager == null)
            {
                await this.InitializeAsync(cancellationToken);
            }
        }
        finally
        {
            this.writeLock.Release();
        }
    }

    private Task InitializeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var indexPath = this.applicationPaths.IndexDirectory;
        this.applicationPaths.EnsureDirectoryExists(indexPath);

        this.directory = FSDirectory.Open(indexPath);
        this.analyzer = new AsciiFoldingAnalyzer(AppLuceneVersion);

        var indexConfig = new IndexWriterConfig(AppLuceneVersion, this.analyzer)
        {
            OpenMode = OpenMode.CREATE_OR_APPEND,
        };

        this.indexWriter = new IndexWriter(this.directory, indexConfig);
        this.searcherManager = new SearcherManager(this.indexWriter, applyAllDeletes: true, null);

        // Write schema version if this is a new index
        if (this.GetIndexSchemaVersion() == null)
        {
            this.WriteSchemaVersion();
        }

        return Task.CompletedTask;
    }

    private async Task DisposeResourcesAsync()
    {
        this.searcherManager?.Dispose();
        this.searcherManager = null;

        if (this.indexWriter != null)
        {
            try
            {
                this.indexWriter.Commit();
            }
            catch (ObjectDisposedException)
            {
                // Index was already closed, ignore
            }

            this.indexWriter.Dispose();
            this.indexWriter = null;
        }

        this.analyzer?.Dispose();
        this.analyzer = null;

        this.directory?.Dispose();
        this.directory = null;

        await Task.CompletedTask;
    }
}
