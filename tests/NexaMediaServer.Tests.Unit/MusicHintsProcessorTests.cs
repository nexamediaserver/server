// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using FluentAssertions;

using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Infrastructure.Services.Music;

using Xunit;

namespace NexaMediaServer.Tests.Unit;

/// <summary>
/// Tests for <see cref="MusicHintsProcessor"/> hint application.
/// </summary>
public class MusicHintsProcessorTests
{
    #region ApplyHintsToTrack Tests

    [Fact]
    public void ApplyHintsToTrackDoesNothingWhenHintsNull()
    {
        var track = new Track();
        MusicHintsProcessor.ApplyHintsToTrack(track, null);
        track.ExtraFields.Should().BeEmpty();
        track.PendingExternalIds.Should().BeEmpty();
    }

    [Fact]
    public void ApplyHintsToTrackDoesNothingWhenHintsEmpty()
    {
        var track = new Track();
        MusicHintsProcessor.ApplyHintsToTrack(track, new Dictionary<string, object>());
        track.ExtraFields.Should().BeEmpty();
        track.PendingExternalIds.Should().BeEmpty();
    }

    [Fact]
    public void ApplyHintsToTrackSetsBpm()
    {
        var track = new Track();
        var hints = new Dictionary<string, object>
        {
            [EmbeddedMetadataHintKeys.Bpm] = "120",
        };

        MusicHintsProcessor.ApplyHintsToTrack(track, hints);

        track.ExtraFields.Should().ContainKey(ExtraFieldKeys.Bpm);
        track.ExtraFields[ExtraFieldKeys.Bpm].Should().Be(120);
    }

    [Fact]
    public void ApplyHintsToTrackSetsMusicalKey()
    {
        var track = new Track();
        var hints = new Dictionary<string, object>
        {
            [EmbeddedMetadataHintKeys.Key] = "C minor",
        };

        MusicHintsProcessor.ApplyHintsToTrack(track, hints);

        track.ExtraFields.Should().ContainKey(ExtraFieldKeys.Key);
        track.ExtraFields[ExtraFieldKeys.Key].Should().Be("C minor");
    }

    [Fact]
    public void ApplyHintsToTrackSetsClassicalMusicFields()
    {
        var track = new Track();
        var hints = new Dictionary<string, object>
        {
            [EmbeddedMetadataHintKeys.Work] = "Symphony No. 9",
            [EmbeddedMetadataHintKeys.Movement] = "Allegro ma non troppo",
            [EmbeddedMetadataHintKeys.MovementNumber] = "1",
            [EmbeddedMetadataHintKeys.MovementTotal] = "4",
            [EmbeddedMetadataHintKeys.ShowMovement] = "1",
        };

        MusicHintsProcessor.ApplyHintsToTrack(track, hints);

        track.ExtraFields.Should().ContainKey(ExtraFieldKeys.WorkTitle);
        track.ExtraFields[ExtraFieldKeys.WorkTitle].Should().Be("Symphony No. 9");
        track.ExtraFields[ExtraFieldKeys.MovementTitle].Should().Be("Allegro ma non troppo");
        track.ExtraFields[ExtraFieldKeys.MovementNumber].Should().Be(1);
        track.ExtraFields[ExtraFieldKeys.MovementTotal].Should().Be(4);
        track.ExtraFields[ExtraFieldKeys.ShowMovement].Should().Be(true);
    }

    [Fact]
    public void ApplyHintsToTrackAddsExternalIds()
    {
        var track = new Track();
        var hints = new Dictionary<string, object>
        {
            [EmbeddedMetadataHintKeys.MusicBrainzTrackId] = "12345678-1234-1234-1234-123456789abc",
            [EmbeddedMetadataHintKeys.Isrc] = "USRC12345678",
        };

        MusicHintsProcessor.ApplyHintsToTrack(track, hints);

        track.PendingExternalIds.Should().Contain((ExternalIdProviders.MusicBrainzTrack, "12345678-1234-1234-1234-123456789abc"));
        track.PendingExternalIds.Should().Contain((ExternalIdProviders.Isrc, "USRC12345678"));
    }

    [Fact]
    public void ApplyHintsToTrackSetsSortNames()
    {
        var track = new Track();
        var hints = new Dictionary<string, object>
        {
            [EmbeddedMetadataHintKeys.TitleSort] = "Stairway to Heaven",
            [EmbeddedMetadataHintKeys.ArtistSort] = "Zeppelin, Led",
        };

        MusicHintsProcessor.ApplyHintsToTrack(track, hints);

        track.ExtraFields[ExtraFieldKeys.TitleSort].Should().Be("Stairway to Heaven");
        track.ExtraFields[ExtraFieldKeys.ArtistSort].Should().Be("Zeppelin, Led");
    }

    #endregion

    #region ApplyHintsToAlbumRelease Tests

    [Fact]
    public void ApplyHintsToAlbumReleaseDoesNothingWhenHintsNull()
    {
        var release = new AlbumRelease();
        MusicHintsProcessor.ApplyHintsToAlbumRelease(release, null);
        release.ExtraFields.Should().BeEmpty();
    }

    [Fact]
    public void ApplyHintsToAlbumReleaseSetsReleaseInfo()
    {
        var release = new AlbumRelease();
        var hints = new Dictionary<string, object>
        {
            [EmbeddedMetadataHintKeys.ReleaseType] = "album",
            [EmbeddedMetadataHintKeys.ReleaseStatus] = "official",
            [EmbeddedMetadataHintKeys.ReleaseCountry] = "US",
            [EmbeddedMetadataHintKeys.Label] = "Atlantic Records",
            [EmbeddedMetadataHintKeys.CatalogNumber] = "SD 19129",
        };

        MusicHintsProcessor.ApplyHintsToAlbumRelease(release, hints);

        release.ExtraFields[ExtraFieldKeys.ReleaseType].Should().Be("album");
        release.ExtraFields[ExtraFieldKeys.ReleaseStatus].Should().Be("official");
        release.ExtraFields[ExtraFieldKeys.ReleaseCountry].Should().Be("US");
        release.ExtraFields[ExtraFieldKeys.LabelName].Should().Be("Atlantic Records");
        release.ExtraFields[ExtraFieldKeys.CatalogNumber].Should().Be("SD 19129");
    }

    [Fact]
    public void ApplyHintsToAlbumReleaseSetsCompilationFlag()
    {
        var release = new AlbumRelease();
        var hints = new Dictionary<string, object>
        {
            [EmbeddedMetadataHintKeys.Compilation] = "1",
        };

        MusicHintsProcessor.ApplyHintsToAlbumRelease(release, hints);

        release.ExtraFields[ExtraFieldKeys.Compilation].Should().Be(true);
    }

    [Fact]
    public void ApplyHintsToAlbumReleaseAddsExternalIds()
    {
        var release = new AlbumRelease();
        var hints = new Dictionary<string, object>
        {
            [EmbeddedMetadataHintKeys.MusicBrainzReleaseId] = "abcd1234-1234-1234-1234-123456789abc",
            [EmbeddedMetadataHintKeys.Barcode] = "075678164224",
        };

        MusicHintsProcessor.ApplyHintsToAlbumRelease(release, hints);

        release.PendingExternalIds.Should().Contain((ExternalIdProviders.MusicBrainzRelease, "abcd1234-1234-1234-1234-123456789abc"));
        release.PendingExternalIds.Should().Contain((ExternalIdProviders.Barcode, "075678164224"));
    }

    #endregion

    #region ApplyHintsToAlbumReleaseGroup Tests

    [Fact]
    public void ApplyHintsToAlbumReleaseGroupSetsReleaseType()
    {
        var releaseGroup = new AlbumReleaseGroup();
        var hints = new Dictionary<string, object>
        {
            [EmbeddedMetadataHintKeys.ReleaseType] = "ep",
        };

        MusicHintsProcessor.ApplyHintsToAlbumReleaseGroup(releaseGroup, hints);

        releaseGroup.ExtraFields[ExtraFieldKeys.ReleaseType].Should().Be("ep");
    }

    [Fact]
    public void ApplyHintsToAlbumReleaseGroupAddsExternalId()
    {
        var releaseGroup = new AlbumReleaseGroup();
        var hints = new Dictionary<string, object>
        {
            [EmbeddedMetadataHintKeys.ExternalIds] = new Dictionary<string, string>
            {
                [ExternalIdProviders.MusicBrainzReleaseGroup] = "group-id-1234",
            },
        };

        MusicHintsProcessor.ApplyHintsToAlbumReleaseGroup(releaseGroup, hints);

        releaseGroup.PendingExternalIds.Should().Contain((ExternalIdProviders.MusicBrainzReleaseGroup, "group-id-1234"));
    }

    #endregion

    #region ApplyHintsToAlbumMedium Tests

    [Fact]
    public void ApplyHintsToAlbumMediumSetsMediaFormat()
    {
        var medium = new AlbumMedium();
        var hints = new Dictionary<string, object>
        {
            [EmbeddedMetadataHintKeys.Media] = "CD",
            [EmbeddedMetadataHintKeys.DiscSubtitle] = "Bonus Disc",
        };

        MusicHintsProcessor.ApplyHintsToAlbumMedium(medium, hints);

        medium.ExtraFields[ExtraFieldKeys.MediaFormat].Should().Be("CD");
        medium.ExtraFields[ExtraFieldKeys.DiscSubtitle].Should().Be("Bonus Disc");
    }

    [Fact]
    public void ApplyHintsToAlbumMediumDoesNotAddExternalIdsDirectly()
    {
        // AlbumMedium only processes Media and DiscSubtitle, disc IDs are handled at release level
        var medium = new AlbumMedium();
        var hints = new Dictionary<string, object>
        {
            [EmbeddedMetadataHintKeys.MusicBrainzDiscId] = "disc-id-xyz",
        };

        MusicHintsProcessor.ApplyHintsToAlbumMedium(medium, hints);

        medium.PendingExternalIds.Should().BeEmpty();
    }

    #endregion

    #region ApplyHintsToAudioWork Tests

    [Fact]
    public void ApplyHintsToAudioWorkSetsWorkTitle()
    {
        var work = new AudioWork();
        var hints = new Dictionary<string, object>
        {
            [EmbeddedMetadataHintKeys.Work] = "Piano Concerto No. 21",
        };

        MusicHintsProcessor.ApplyHintsToAudioWork(work, hints);

        // Work title is set on the Title property when empty
        work.Title.Should().Be("Piano Concerto No. 21");
    }

    [Fact]
    public void ApplyHintsToAudioWorkAddsExternalId()
    {
        var work = new AudioWork();
        var hints = new Dictionary<string, object>
        {
            [EmbeddedMetadataHintKeys.MusicBrainzWorkId] = "work-id-abc",
        };

        MusicHintsProcessor.ApplyHintsToAudioWork(work, hints);

        work.PendingExternalIds.Should().Contain((ExternalIdProviders.MusicBrainzWork, "work-id-abc"));
    }

    #endregion

    #region ExtractPersonCredits Tests

    [Fact]
    public void ExtractPersonCreditsReturnsEmptyWhenNoCredits()
    {
        var hints = new Dictionary<string, object>
        {
            [EmbeddedMetadataHintKeys.Album] = "Some Album",
        };

        var credits = MusicHintsProcessor.ExtractPersonCredits(hints);

        credits.Should().BeEmpty();
    }

    [Fact]
    public void ExtractPersonCreditsExtractsComposer()
    {
        var hints = new Dictionary<string, object>
        {
            [EmbeddedMetadataHintKeys.Composers] = "Ludwig van Beethoven",
        };

        var credits = MusicHintsProcessor.ExtractPersonCredits(hints);

        credits.Should().ContainSingle();
        credits[0].Person.Title.Should().Be("Ludwig van Beethoven");
        credits[0].RelationType.Should().Be(RelationType.PersonComposesAudio);
    }

    [Fact]
    public void ExtractPersonCreditsExtractsConductor()
    {
        var hints = new Dictionary<string, object>
        {
            [EmbeddedMetadataHintKeys.Conductors] = "Herbert von Karajan",
        };

        var credits = MusicHintsProcessor.ExtractPersonCredits(hints);

        credits.Should().ContainSingle();
        credits[0].Person.Title.Should().Be("Herbert von Karajan");
        credits[0].RelationType.Should().Be(RelationType.PersonConductsAudio);
    }

    [Fact]
    public void ExtractPersonCreditsExtractsMultipleArtists()
    {
        var hints = new Dictionary<string, object>
        {
            [EmbeddedMetadataHintKeys.Composers] = new List<object> { "John Lennon", "Paul McCartney" },
        };

        var credits = MusicHintsProcessor.ExtractPersonCredits(hints);

        credits.Should().HaveCount(2);
        credits.Select(c => c.Person.Title).Should().Contain("John Lennon");
        credits.Select(c => c.Person.Title).Should().Contain("Paul McCartney");
    }

    [Fact]
    public void ExtractPersonCreditsHandlesPerformerWithRole()
    {
        var hints = new Dictionary<string, object>
        {
            [EmbeddedMetadataHintKeys.PerformerCredits] = new Dictionary<string, object>
            {
                ["guitar"] = "Jimmy Page",
            },
        };

        var credits = MusicHintsProcessor.ExtractPersonCredits(hints);

        credits.Should().ContainSingle();
        credits[0].Person.Title.Should().Be("Jimmy Page");
        credits[0].Text.Should().Be("guitar");
        credits[0].RelationType.Should().Be(RelationType.PersonPerformsInstrumentOrVocals);
    }

    #endregion
}
