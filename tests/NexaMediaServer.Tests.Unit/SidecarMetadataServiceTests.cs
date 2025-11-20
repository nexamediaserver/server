// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CA1707 // Identifiers should not contain underscores

using Microsoft.Extensions.Logging.Abstractions;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Services;
using NexaMediaServer.Infrastructure.Services.Metadata;
using Xunit;

namespace NexaMediaServer.Tests.Unit;

public class SidecarMetadataServiceTests
{
    [Fact]
    public void ApplyOverlay_WithContentRating_ResolvesAge()
    {
        // Arrange
        var contentRatingService = new ContentRatingService(
            NullLogger<ContentRatingService>.Instance
        );
        var target = new MetadataItem { MetadataType = MetadataType.Movie, Title = "Test" };

        var overlay = new Movie
        {
            Title = "Test Movie",
            SortTitle = "Test Movie",
            ContentRating = "PG-13",
            ContentRatingCountryCode = "US",
        };

        // Use reflection to access the private ApplyOverlay method
        var service = new TestableSidecarMetadataService(contentRatingService);

        // Act
        var changed = service.PublicApplyOverlay(target, overlay);

        // Assert
        Assert.True(changed);
        Assert.Equal("PG-13", target.ContentRating);
        Assert.Equal(13, target.ContentRatingAge);
    }

    [Fact]
    public void ApplyOverlay_WithContentRatingNoCountry_ResolvesAgeViaFallback()
    {
        // Arrange
        var contentRatingService = new ContentRatingService(
            NullLogger<ContentRatingService>.Instance
        );
        var target = new MetadataItem { MetadataType = MetadataType.Movie, Title = "Test" };

        var overlay = new Movie
        {
            Title = "Test Movie",
            SortTitle = "Test Movie",
            ContentRating = "R",
            ContentRatingCountryCode = null, // No country code
        };

        var service = new TestableSidecarMetadataService(contentRatingService);

        // Act
        var changed = service.PublicApplyOverlay(target, overlay);

        // Assert
        Assert.True(changed);
        Assert.Equal("R", target.ContentRating);
        Assert.Equal(17, target.ContentRatingAge); // Should resolve via fallback table
    }

    [Fact]
    public void ApplyOverlay_WithUnknownContentRating_DoesNotSetAge()
    {
        // Arrange
        var contentRatingService = new ContentRatingService(
            NullLogger<ContentRatingService>.Instance
        );
        var target = new MetadataItem { MetadataType = MetadataType.Movie, Title = "Test" };

        var overlay = new Movie
        {
            Title = "Test Movie",
            SortTitle = "Test Movie",
            ContentRating = "UNKNOWN-RATING",
            ContentRatingCountryCode = "XX",
        };

        var service = new TestableSidecarMetadataService(contentRatingService);

        // Act
        var changed = service.PublicApplyOverlay(target, overlay);

        // Assert
        Assert.True(changed); // Title changed
        Assert.Equal("UNKNOWN-RATING", target.ContentRating);
        Assert.Null(target.ContentRatingAge); // Should remain null for unknown ratings
    }

    [Fact]
    public void ApplyOverlay_WithTvShowRating_UsesTvRatingTable()
    {
        // Arrange
        var contentRatingService = new ContentRatingService(
            NullLogger<ContentRatingService>.Instance
        );
        var target = new MetadataItem { MetadataType = MetadataType.Show, Title = "Test" };

        var overlay = new Show
        {
            Title = "Test Show",
            SortTitle = "Test Show",
            ContentRating = "TV-MA",
            ContentRatingCountryCode = "US",
        };

        var service = new TestableSidecarMetadataService(contentRatingService);

        // Act
        var changed = service.PublicApplyOverlay(target, overlay);

        // Assert
        Assert.True(changed);
        Assert.Equal("TV-MA", target.ContentRating);
        Assert.Equal(17, target.ContentRatingAge); // TV-MA for US is 17
    }

    // Expose the private ApplyOverlay method for testing
    private sealed class TestableSidecarMetadataService(IContentRatingService contentRatingService)
    {
        private readonly ContentRatingService _contentRatingService =
            (ContentRatingService)contentRatingService;

        public bool PublicApplyOverlay(MetadataItem target, MetadataBaseItem? overlay)
        {
            if (overlay is null)
            {
                return false;
            }

            var changed = false;

            // Apply title
            changed |= AssignIfChanged(
                target.Title,
                PreferString(overlay.Title, target.Title),
                value => target.Title = value
            );

            // Apply sort title
            changed |= AssignIfChanged(
                target.SortTitle,
                PreferString(overlay.SortTitle, target.SortTitle),
                value => target.SortTitle = value
            );

            // Apply content rating
            changed |= AssignIfChanged(
                target.ContentRating,
                PreferOptionalString(overlay.ContentRating, target.ContentRating),
                value => target.ContentRating = value
            );

            // Resolve content rating age if rating is provided
            if (!string.IsNullOrWhiteSpace(overlay.ContentRating))
            {
                var isTelevision =
                    target.MetadataType
                    is MetadataType.Show
                        or MetadataType.Season
                        or MetadataType.Episode;
                var resolvedAge = this._contentRatingService.ResolveAge(
                    overlay.ContentRating,
                    overlay.ContentRatingCountryCode,
                    isTelevision
                );
                changed |= AssignIfChanged(
                    target.ContentRatingAge,
                    resolvedAge ?? target.ContentRatingAge,
                    value => target.ContentRatingAge = value
                );
            }
            else
            {
                changed |= AssignIfChanged(
                    target.ContentRatingAge,
                    overlay.ContentRatingAge ?? target.ContentRatingAge,
                    value => target.ContentRatingAge = value
                );
            }

            return changed;
        }

        private static string PreferString(string incoming, string current) =>
            string.IsNullOrWhiteSpace(incoming) ? current : incoming.Trim();

        private static string? PreferOptionalString(string? incoming, string? current) =>
            string.IsNullOrWhiteSpace(incoming) ? current : incoming.Trim();

        private static bool AssignIfChanged<T>(T current, T next, Action<T> setter)
        {
            if (EqualityComparer<T>.Default.Equals(current, next))
            {
                return false;
            }

            setter(next);
            return true;
        }
    }
}
