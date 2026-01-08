// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CA1707 // Identifiers should not contain underscores

using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Infrastructure.Services.Metadata;
using NexaMediaServer.Infrastructure.Services.Resolvers;

using Xunit;

namespace NexaMediaServer.Tests.Unit;

public class LocalArtworkParserTests : IDisposable
{
    private readonly string _tempDir;
    private readonly LocalArtworkParser _parser;

    public LocalArtworkParserTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"local_artwork_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _parser = new LocalArtworkParser();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }

        GC.SuppressFinalize(this);
    }

    #region CanParse Tests

    [Theory]
    [InlineData(".jpg", true)]
    [InlineData(".jpeg", true)]
    [InlineData(".png", true)]
    [InlineData(".webp", true)]
    [InlineData(".gif", true)]
    [InlineData(".tbn", true)]
    [InlineData(".JPG", true)] // Case-insensitive
    [InlineData(".PNG", true)]
    [InlineData(".mp4", false)]
    [InlineData(".mkv", false)]
    [InlineData(".nfo", false)]
    [InlineData("", false)]
    public void CanParse_WithExtension_ReturnsExpected(string extension, bool expected)
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, $"test{extension}");
        File.WriteAllText(filePath, string.Empty);
        var file = FileSystemMetadata.FromPath(filePath);

        // Act
        var result = _parser.CanParse(file);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CanParse_WithNonExistentFile_ReturnsFalse()
    {
        // Arrange
        var file = FileSystemMetadata.FromPath(Path.Combine(_tempDir, "nonexistent.jpg"));

        // Act
        var result = _parser.CanParse(file);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanParse_WithDirectory_ReturnsFalse()
    {
        // Arrange
        var subDir = Path.Combine(_tempDir, "subdir.jpg");
        Directory.CreateDirectory(subDir);
        var file = FileSystemMetadata.FromPath(subDir);

        // Act
        var result = _parser.CanParse(file);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Poster Detection Tests

    [Fact]
    public async Task ParseAsync_WithPosterJpg_ExtractsPoster()
    {
        // Arrange
        var posterPath = CreateFile("poster.jpg");
        var mediaPath = CreateFile("Movie.mp4");
        var request = CreateRequest(mediaPath, posterPath);

        // Act
        var result = await _parser.ParseAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var movie = Assert.IsType<Movie>(result.Metadata);
        Assert.Equal(posterPath, movie.ThumbUri);
        Assert.Null(movie.ArtUri);
        Assert.Null(movie.LogoUri);
    }

    [Theory]
    [InlineData("poster.jpg")]
    [InlineData("poster.jpeg")]
    [InlineData("poster.png")]
    [InlineData("poster.webp")]
    [InlineData("poster.gif")]
    [InlineData("poster.tbn")]
    [InlineData("POSTER.JPG")] // Case-insensitive
    [InlineData("cover.jpg")]
    [InlineData("folder.jpg")]
    [InlineData("movie.jpg")]
    public async Task ParseAsync_WithCanonicalPosterNames_ExtractsPoster(string posterFileName)
    {
        // Arrange
        var posterPath = CreateFile(posterFileName);
        var mediaPath = CreateFile("TestMovie.mp4");
        var request = CreateRequest(mediaPath, posterPath);

        // Act
        var result = await _parser.ParseAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var movie = Assert.IsType<Movie>(result.Metadata);
        Assert.Equal(posterPath, movie.ThumbUri);
    }

    [Fact]
    public async Task ParseAsync_WithMovieNamePoster_ExtractsPoster()
    {
        // Arrange
        var posterPath = CreateFile("TestMovie-poster.jpg");
        var mediaPath = CreateFile("TestMovie.mp4");
        var request = CreateRequest(mediaPath, posterPath);

        // Act
        var result = await _parser.ParseAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var movie = Assert.IsType<Movie>(result.Metadata);
        Assert.Equal(posterPath, movie.ThumbUri);
    }

    [Fact]
    public async Task ParseAsync_WithPlexFallbackPoster_ExtractsPoster()
    {
        // Arrange: Plex supports MovieName.jpg as poster fallback
        var posterPath = CreateFile("TestMovie.jpg");
        var mediaPath = CreateFile("TestMovie.mp4");
        var request = CreateRequest(mediaPath, posterPath);

        // Act
        var result = await _parser.ParseAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var movie = Assert.IsType<Movie>(result.Metadata);
        Assert.Equal(posterPath, movie.ThumbUri);
    }

    [Fact]
    public async Task ParseAsync_PrefersCanonicalOverNamedPoster()
    {
        // Arrange: poster.jpg takes precedence over MovieName-poster.jpg
        var canonicalPoster = CreateFile("poster.jpg");
        var namedPoster = CreateFile("TestMovie-poster.jpg");
        var mediaPath = CreateFile("TestMovie.mp4");
        var request = CreateRequest(mediaPath, canonicalPoster);

        // Act
        var result = await _parser.ParseAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var movie = Assert.IsType<Movie>(result.Metadata);
        Assert.Equal(canonicalPoster, movie.ThumbUri);
    }

    [Fact]
    public async Task ParseAsync_PrefersNamedOverPlexFallback()
    {
        // Arrange: MovieName-poster.jpg takes precedence over MovieName.jpg
        var namedPoster = CreateFile("TestMovie-poster.jpg");
        var plexFallback = CreateFile("TestMovie.jpg");
        var mediaPath = CreateFile("TestMovie.mp4");

        // Create request without the canonical poster
        var sidecarFile = FileSystemMetadata.FromPath(namedPoster);
        var mediaFile = FileSystemMetadata.FromPath(mediaPath);
        var request = new SidecarParseRequest(mediaFile, sidecarFile, LibraryType.Movies);

        // Act
        var result = await _parser.ParseAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var movie = Assert.IsType<Movie>(result.Metadata);
        Assert.Equal(namedPoster, movie.ThumbUri);
    }

    #endregion

    #region Backdrop Detection Tests

    [Theory]
    [InlineData("fanart.jpg")]
    [InlineData("backdrop.jpg")]
    [InlineData("background.jpg")]
    [InlineData("art.jpg")]
    [InlineData("FANART.PNG")] // Case-insensitive
    public async Task ParseAsync_WithCanonicalBackdropNames_ExtractsBackdrop(
        string backdropFileName
    )
    {
        // Arrange
        var backdropPath = CreateFile(backdropFileName);
        var mediaPath = CreateFile("TestMovie.mp4");
        var request = CreateRequest(mediaPath, backdropPath);

        // Act
        var result = await _parser.ParseAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var movie = Assert.IsType<Movie>(result.Metadata);
        Assert.Equal(backdropPath, movie.ArtUri);
        Assert.Null(movie.ThumbUri); // No poster
    }

    [Fact]
    public async Task ParseAsync_WithMovieNameFanart_ExtractsBackdrop()
    {
        // Arrange
        var backdropPath = CreateFile("TestMovie-fanart.jpg");
        var mediaPath = CreateFile("TestMovie.mp4");
        var request = CreateRequest(mediaPath, backdropPath);

        // Act
        var result = await _parser.ParseAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var movie = Assert.IsType<Movie>(result.Metadata);
        Assert.Equal(backdropPath, movie.ArtUri);
    }

    [Fact]
    public async Task ParseAsync_PrefersCanonicalOverNamedBackdrop()
    {
        // Arrange: fanart.jpg takes precedence over MovieName-fanart.jpg
        var canonicalBackdrop = CreateFile("fanart.jpg");
        var namedBackdrop = CreateFile("TestMovie-fanart.jpg");
        var mediaPath = CreateFile("TestMovie.mp4");
        var request = CreateRequest(mediaPath, canonicalBackdrop);

        // Act
        var result = await _parser.ParseAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var movie = Assert.IsType<Movie>(result.Metadata);
        Assert.Equal(canonicalBackdrop, movie.ArtUri);
    }

    [Fact]
    public async Task ParseAsync_DoesNotUsePlexFallbackForBackdrop()
    {
        // Arrange: MovieName.jpg should NOT be used as backdrop (Plex fallback is poster-only)
        var plexFallback = CreateFile("TestMovie.jpg");
        var mediaPath = CreateFile("TestMovie.mp4");
        var request = CreateRequest(mediaPath, plexFallback);

        // Act
        var result = await _parser.ParseAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var movie = Assert.IsType<Movie>(result.Metadata);
        Assert.Equal(plexFallback, movie.ThumbUri); // Used as poster
        Assert.Null(movie.ArtUri); // NOT as backdrop
    }

    #endregion

    #region Logo Detection Tests

    [Theory]
    [InlineData("logo.png")]
    [InlineData("clearlogo.png")]
    [InlineData("LOGO.PNG")] // Case-insensitive
    public async Task ParseAsync_WithCanonicalLogoNames_ExtractsLogo(string logoFileName)
    {
        // Arrange
        var logoPath = CreateFile(logoFileName);
        var mediaPath = CreateFile("TestMovie.mp4");
        var request = CreateRequest(mediaPath, logoPath);

        // Act
        var result = await _parser.ParseAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var movie = Assert.IsType<Movie>(result.Metadata);
        Assert.Equal(logoPath, movie.LogoUri);
        Assert.Null(movie.ThumbUri);
        Assert.Null(movie.ArtUri);
    }

    #endregion

    #region Combined Artwork Tests

    [Fact]
    public async Task ParseAsync_WithAllArtworkTypes_ExtractsAll()
    {
        // Arrange
        var posterPath = CreateFile("poster.jpg");
        var fanartPath = CreateFile("fanart.jpg");
        var logoPath = CreateFile("logo.png");
        var mediaPath = CreateFile("TestMovie.mp4");
        var request = CreateRequest(mediaPath, posterPath);

        // Act
        var result = await _parser.ParseAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var movie = Assert.IsType<Movie>(result.Metadata);
        Assert.Equal(posterPath, movie.ThumbUri);
        Assert.Equal(fanartPath, movie.ArtUri);
        Assert.Equal(logoPath, movie.LogoUri);
    }

    #endregion

    #region Library Type Tests

    [Fact]
    public async Task ParseAsync_WithNonMoviesLibrary_ReturnsNull()
    {
        // Arrange
        var posterPath = CreateFile("poster.jpg");
        var mediaPath = CreateFile("Show.mp4");

        var sidecarFile = FileSystemMetadata.FromPath(posterPath);
        var mediaFile = FileSystemMetadata.FromPath(mediaPath);
        var request = new SidecarParseRequest(mediaFile, sidecarFile, LibraryType.TVShows);

        // Act
        var result = await _parser.ParseAsync(request, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Siblings (Batch Processing) Tests

    [Fact]
    public async Task ParseAsync_WithSiblings_UsesPreEnumeratedFiles()
    {
        // Arrange
        var posterPath = CreateFile("poster.jpg");
        var fanartPath = CreateFile("fanart.jpg");
        var mediaPath = CreateFile("TestMovie.mp4");
        var nfoPath = CreateFile("movie.nfo");

        var siblings = new List<FileSystemMetadata>
        {
            FileSystemMetadata.FromPath(posterPath),
            FileSystemMetadata.FromPath(fanartPath),
            FileSystemMetadata.FromPath(nfoPath),
        };

        var sidecarFile = FileSystemMetadata.FromPath(posterPath);
        var mediaFile = FileSystemMetadata.FromPath(mediaPath);
        var request = new SidecarParseRequest(mediaFile, sidecarFile, LibraryType.Movies, siblings);

        // Act
        var result = await _parser.ParseAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var movie = Assert.IsType<Movie>(result.Metadata);
        Assert.Equal(posterPath, movie.ThumbUri);
        Assert.Equal(fanartPath, movie.ArtUri);
    }

    [Fact]
    public async Task ParseAsync_WithEmptySiblings_FallsBackToDirectoryScan()
    {
        // Arrange: Empty siblings should fall back to directory scanning
        var posterPath = CreateFile("poster.jpg");
        var mediaPath = CreateFile("TestMovie.mp4");

        var sidecarFile = FileSystemMetadata.FromPath(posterPath);
        var mediaFile = FileSystemMetadata.FromPath(mediaPath);
        var request = new SidecarParseRequest(
            mediaFile,
            sidecarFile,
            LibraryType.Movies,
            Siblings: [] // Empty list
        );

        // Act
        var result = await _parser.ParseAsync(request, CancellationToken.None);

        // Assert - Falls back to directory scan
        Assert.NotNull(result);
        var movie = Assert.IsType<Movie>(result.Metadata);
        Assert.Equal(posterPath, movie.ThumbUri);
    }

    #endregion

    #region No Artwork Tests

    [Fact]
    public async Task ParseAsync_WithNoMatchingArtwork_ReturnsNull()
    {
        // Arrange
        var mediaPath = CreateFile("TestMovie.mp4");
        CreateFile("random-image.jpg"); // Not a recognized artwork name

        var sidecarFile = FileSystemMetadata.FromPath(Path.Combine(_tempDir, "random-image.jpg"));
        var mediaFile = FileSystemMetadata.FromPath(mediaPath);
        var request = new SidecarParseRequest(mediaFile, sidecarFile, LibraryType.Movies);

        // Act
        var result = await _parser.ParseAsync(request, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ParseAsync_WithNoImages_ReturnsNull()
    {
        // Arrange
        var mediaPath = CreateFile("TestMovie.mp4");
        var nfoPath = CreateFile("movie.nfo");

        var sidecarFile = FileSystemMetadata.FromPath(nfoPath);
        var mediaFile = FileSystemMetadata.FromPath(mediaPath);

        // Use siblings with only non-image files
        var siblings = new List<FileSystemMetadata> { FileSystemMetadata.FromPath(nfoPath) };
        var request = new SidecarParseRequest(mediaFile, sidecarFile, LibraryType.Movies, siblings);

        // Act
        var result = await _parser.ParseAsync(request, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region Parser Metadata Tests

    [Fact]
    public void Name_ReturnsCorrectIdentifier()
    {
        // Assert
        Assert.Equal("local-artwork", _parser.Name);
    }

    [Fact]
    public async Task ParseAsync_SetsSourceInResult()
    {
        // Arrange
        var posterPath = CreateFile("poster.jpg");
        var mediaPath = CreateFile("Movie.mp4");
        var request = CreateRequest(mediaPath, posterPath);

        // Act
        var result = await _parser.ParseAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("local-artwork", result.Source);
    }

    #endregion

    #region Helper Methods

    private string CreateFile(string fileName)
    {
        var filePath = Path.Combine(_tempDir, fileName);
        File.WriteAllText(filePath, string.Empty);
        return filePath;
    }

    private static SidecarParseRequest CreateRequest(string mediaPath, string sidecarPath)
    {
        var sidecarFile = FileSystemMetadata.FromPath(sidecarPath);
        var mediaFile = FileSystemMetadata.FromPath(mediaPath);
        return new SidecarParseRequest(mediaFile, sidecarFile, LibraryType.Movies);
    }

    #endregion
}
