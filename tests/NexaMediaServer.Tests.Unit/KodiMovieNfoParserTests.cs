// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CA1707 // Identifiers should not contain underscores

using NexaMediaServer.Core.Enums;
using NexaMediaServer.Infrastructure.Services.Metadata;
using NexaMediaServer.Infrastructure.Services.Resolvers;
using Xunit;

namespace NexaMediaServer.Tests.Unit;

public class KodiMovieNfoParserTests : IDisposable
{
    private readonly string _tempDir;
    private readonly KodiMovieNfoParser _parser;

    public KodiMovieNfoParserTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"kodi_nfo_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _parser = new KodiMovieNfoParser();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ParseAsync_WithContentRating_ExtractsRatingAndCountryCode()
    {
        // Arrange
        var nfoContent =
            @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<movie>
    <title>Test Movie</title>
    <mpaa>US:PG-13</mpaa>
</movie>";
        var nfoPath = Path.Combine(_tempDir, "movie.nfo");
        var mediaPath = Path.Combine(_tempDir, "movie.mp4");
        await File.WriteAllTextAsync(nfoPath, nfoContent);
        await File.WriteAllTextAsync(mediaPath, string.Empty);

        var sidecarFile = FileSystemMetadata.FromPath(nfoPath);
        var mediaFile = FileSystemMetadata.FromPath(mediaPath);
        var request = new SidecarParseRequest(mediaFile, sidecarFile, LibraryType.Movies);

        // Act
        var result = await _parser.ParseAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Metadata);
        Assert.Equal("PG-13", result.Metadata.ContentRating);
        Assert.Equal("US", result.Metadata.ContentRatingCountryCode);
    }

    [Theory]
    [InlineData("US:PG-13", "PG-13", "US")]
    [InlineData("UK:15", "15", "UK")]
    [InlineData("[US] R", "R", "US")]
    [InlineData("PG-13 (US)", "PG-13", "US")]
    [InlineData("PG-13 / UK:12A", "PG-13", null)] // Multi-country, first part only
    [InlineData("PG-13", "PG-13", null)] // No country code
    public async Task ParseAsync_ContentRatingFormats_ParsesCorrectly(
        string mpaaRating,
        string expectedRating,
        string? expectedCountryCode
    )
    {
        // Arrange
        var nfoContent =
            $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<movie>
    <title>Test Movie</title>
    <mpaa>{mpaaRating}</mpaa>
</movie>";
        var nfoPath = Path.Combine(_tempDir, $"movie_{Guid.NewGuid()}.nfo");
        var mediaPath = Path.Combine(_tempDir, $"movie_{Guid.NewGuid()}.mp4");
        await File.WriteAllTextAsync(nfoPath, nfoContent);
        await File.WriteAllTextAsync(mediaPath, string.Empty);

        var sidecarFile = FileSystemMetadata.FromPath(nfoPath);
        var mediaFile = FileSystemMetadata.FromPath(mediaPath);
        var request = new SidecarParseRequest(mediaFile, sidecarFile, LibraryType.Movies);

        // Act
        var result = await _parser.ParseAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Metadata);
        Assert.Equal(expectedRating, result.Metadata.ContentRating);
        Assert.Equal(expectedCountryCode, result.Metadata.ContentRatingCountryCode);
    }

    [Fact]
    public async Task ParseAsync_WithCertification_UsesItAsFallback()
    {
        // Arrange
        var nfoContent =
            @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<movie>
    <title>Test Movie</title>
    <certification>UK:15</certification>
</movie>";
        var nfoPath = Path.Combine(_tempDir, "movie_cert.nfo");
        var mediaPath = Path.Combine(_tempDir, "movie_cert.mp4");
        await File.WriteAllTextAsync(nfoPath, nfoContent);
        await File.WriteAllTextAsync(mediaPath, string.Empty);

        var sidecarFile = FileSystemMetadata.FromPath(nfoPath);
        var mediaFile = FileSystemMetadata.FromPath(mediaPath);
        var request = new SidecarParseRequest(mediaFile, sidecarFile, LibraryType.Movies);

        // Act
        var result = await _parser.ParseAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Metadata);
        Assert.Equal("15", result.Metadata.ContentRating);
        Assert.Equal("UK", result.Metadata.ContentRatingCountryCode);
    }

    [Fact]
    public async Task ParseAsync_MpaaPreferredOverCertification()
    {
        // Arrange
        var nfoContent =
            @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<movie>
    <title>Test Movie</title>
    <mpaa>US:R</mpaa>
    <certification>UK:15</certification>
</movie>";
        var nfoPath = Path.Combine(_tempDir, "movie_both.nfo");
        var mediaPath = Path.Combine(_tempDir, "movie_both.mp4");
        await File.WriteAllTextAsync(nfoPath, nfoContent);
        await File.WriteAllTextAsync(mediaPath, string.Empty);

        var sidecarFile = FileSystemMetadata.FromPath(nfoPath);
        var mediaFile = FileSystemMetadata.FromPath(mediaPath);
        var request = new SidecarParseRequest(mediaFile, sidecarFile, LibraryType.Movies);

        // Act
        var result = await _parser.ParseAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Metadata);
        Assert.Equal("R", result.Metadata.ContentRating);
        Assert.Equal("US", result.Metadata.ContentRatingCountryCode);
    }
}
