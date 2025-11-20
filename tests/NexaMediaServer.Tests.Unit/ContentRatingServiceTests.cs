// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CA1707 // Identifiers should not contain underscores

using Microsoft.Extensions.Logging.Abstractions;
using NexaMediaServer.Infrastructure.Services;
using Xunit;

namespace NexaMediaServer.Tests.Unit;

public class ContentRatingServiceTests
{
    private readonly ContentRatingService _service;

    public ContentRatingServiceTests()
    {
        _service = new ContentRatingService(NullLogger<ContentRatingService>.Instance);
    }

    [Theory]
    [InlineData("PG-13", "US", false, 13)]
    [InlineData("R", "US", false, 17)]
    [InlineData("NC-17", "US", false, 18)]
    [InlineData("G", "US", false, 0)]
    [InlineData("PG", "US", false, 8)]
    public void ResolveAge_UsMovieRatings_ReturnsCorrectAge(
        string rating,
        string countryCode,
        bool isTelevision,
        int expectedAge
    )
    {
        var result = _service.ResolveAge(rating, countryCode, isTelevision);

        Assert.Equal(expectedAge, result);
    }

    [Theory]
    [InlineData("TV-MA", "US", true, 17)]
    [InlineData("TV-14", "US", true, 14)]
    [InlineData("TV-PG", "US", true, 8)]
    [InlineData("TV-G", "US", true, 0)]
    [InlineData("TV-Y7", "US", true, 7)]
    [InlineData("TV-Y", "US", true, 0)]
    public void ResolveAge_UsTvRatings_ReturnsCorrectAge(
        string rating,
        string countryCode,
        bool isTelevision,
        int expectedAge
    )
    {
        var result = _service.ResolveAge(rating, countryCode, isTelevision);

        Assert.Equal(expectedAge, result);
    }

    [Theory]
    [InlineData("15", "UK", false, 15)]
    [InlineData("18", "UK", false, 18)]
    [InlineData("12A", "UK", false, 12)]
    [InlineData("12", "UK", false, 12)]
    [InlineData("PG", "UK", false, 8)]
    [InlineData("U", "UK", false, 0)]
    public void ResolveAge_UkRatings_ReturnsCorrectAge(
        string rating,
        string countryCode,
        bool isTelevision,
        int expectedAge
    )
    {
        var result = _service.ResolveAge(rating, countryCode, isTelevision);

        Assert.Equal(expectedAge, result);
    }

    [Theory]
    [InlineData("R15+", "JP", false, 15)]
    [InlineData("R18+", "JP", false, 18)]
    [InlineData("PG12", "JP", false, 12)]
    [InlineData("G", "JP", false, 0)]
    public void ResolveAge_JapanRatings_ReturnsCorrectAge(
        string rating,
        string countryCode,
        bool isTelevision,
        int expectedAge
    )
    {
        var result = _service.ResolveAge(rating, countryCode, isTelevision);

        Assert.Equal(expectedAge, result);
    }

    [Theory]
    [InlineData("FSK16", "DE", false, 16)]
    [InlineData("FSK18", "DE", false, 18)]
    [InlineData("FSK12", "DE", false, 12)]
    [InlineData("FSK6", "DE", false, 6)]
    [InlineData("FSK0", "DE", false, 0)]
    public void ResolveAge_GermanyRatings_ReturnsCorrectAge(
        string rating,
        string countryCode,
        bool isTelevision,
        int expectedAge
    )
    {
        var result = _service.ResolveAge(rating, countryCode, isTelevision);

        Assert.Equal(expectedAge, result);
    }

    [Theory]
    [InlineData("PG-13", null, false, 13)]
    [InlineData("R", null, false, 17)]
    [InlineData("TV-MA", null, true, 17)]
    [InlineData("15", null, false, 15)]
    [InlineData("18", null, false, 18)]
    [InlineData("12A", null, false, 12)]
    public void ResolveAge_NoCountryCode_FallsBackToCommonPatterns(
        string rating,
        string? countryCode,
        bool isTelevision,
        int expectedAge
    )
    {
        var result = _service.ResolveAge(rating, countryCode, isTelevision);

        Assert.Equal(expectedAge, result);
    }

    [Theory]
    [InlineData("pg-13", "us", false, 13)]
    [InlineData("R", "us", false, 17)]
    [InlineData("tv-ma", "US", true, 17)]
    [InlineData("fsk16", "de", false, 16)]
    public void ResolveAge_NormalizesCountryCodeToUpperCase(
        string rating,
        string countryCode,
        bool isTelevision,
        int expectedAge
    )
    {
        var result = _service.ResolveAge(rating, countryCode, isTelevision);

        Assert.Equal(expectedAge, result);
    }

    [Theory]
    [InlineData("UNKNOWN-RATING", "US", false)]
    [InlineData("CUSTOM", null, false)]
    [InlineData("", "US", false)]
    [InlineData(null, "US", false)]
    public void ResolveAge_UnknownRating_ReturnsNull(
        string? rating,
        string? countryCode,
        bool isTelevision
    )
    {
        var result = _service.ResolveAge(rating!, countryCode, isTelevision);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("PG-13", "XX", false, 13)] // Unknown country, fallback to pattern
    [InlineData("TV-MA", "UNKNOWN", true, 17)] // Unknown country, fallback to pattern
    public void ResolveAge_UnknownCountryCode_FallsBackToPatterns(
        string rating,
        string countryCode,
        bool isTelevision,
        int expectedAge
    )
    {
        var result = _service.ResolveAge(rating, countryCode, isTelevision);

        Assert.Equal(expectedAge, result);
    }

    [Theory]
    [InlineData("  PG-13  ", "US", false, 13)]
    [InlineData("R   ", "  US  ", false, 17)]
    public void ResolveAge_TrimsWhitespace(
        string rating,
        string countryCode,
        bool isTelevision,
        int expectedAge
    )
    {
        var result = _service.ResolveAge(rating, countryCode, isTelevision);

        Assert.Equal(expectedAge, result);
    }

    [Theory]
    [InlineData("MA15+", "AU", false, 15)]
    [InlineData("R18+", "AU", false, 18)]
    [InlineData("M", "AU", false, 15)]
    [InlineData("PG", "AU", false, 8)]
    [InlineData("G", "AU", false, 0)]
    public void ResolveAge_AustraliaRatings_ReturnsCorrectAge(
        string rating,
        string countryCode,
        bool isTelevision,
        int expectedAge
    )
    {
        var result = _service.ResolveAge(rating, countryCode, isTelevision);

        Assert.Equal(expectedAge, result);
    }

    [Theory]
    [InlineData("18", "BR", false, 18)]
    [InlineData("16", "BR", false, 16)]
    [InlineData("14", "BR", false, 14)]
    [InlineData("12", "BR", false, 12)]
    [InlineData("10", "BR", false, 10)]
    [InlineData("L", "BR", false, 0)]
    public void ResolveAge_BrazilRatings_ReturnsCorrectAge(
        string rating,
        string countryCode,
        bool isTelevision,
        int expectedAge
    )
    {
        var result = _service.ResolveAge(rating, countryCode, isTelevision);

        Assert.Equal(expectedAge, result);
    }
}
