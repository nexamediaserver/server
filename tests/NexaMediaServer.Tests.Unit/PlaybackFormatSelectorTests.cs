// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;

using FluentAssertions;

using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Playback;

using Xunit;

namespace NexaMediaServer.Tests.Unit;

/// <summary>
/// Tests for image format selection and media type resolution.
/// </summary>
public class PlaybackFormatSelectorTests
{
    private static readonly string[] WebpAndJpeg = new[] { "webp", "jpg" };
    private static readonly string[] WebpAndJpg = new[] { "webp", "jpg" };
    private static readonly string[] JpegOnly = new[] { "jpg" };
    /// <summary>
    /// Keeps the original format when the client supports it, even if WebP is also supported.
    /// </summary>
    [Fact]
    public void ChooseImageFormatKeepsOriginalWhenSupported()
    {
        var result = PlaybackFormatSelector.ChooseImageFormat("jpg", WebpAndJpeg);

        result.Should().Be("jpg");
    }

    /// <summary>
    /// Prefers WebP when the original format is unsupported but WebP is available.
    /// </summary>
    [Fact]
    public void ChooseImageFormatPrefersWebpWhenOriginalUnsupported()
    {
        var result = PlaybackFormatSelector.ChooseImageFormat("png", WebpAndJpg);

        result.Should().Be("webp");
    }

    /// <summary>
    /// Falls back to JPEG when WebP is unavailable but JPEG is supported.
    /// </summary>
    [Fact]
    public void ChooseImageFormatPrefersJpegWhenWebpMissing()
    {
        var result = PlaybackFormatSelector.ChooseImageFormat("bmp", JpegOnly);

        result.Should().Be("jpg");
    }

    /// <summary>
    /// Keeps the source format when the client declares no supported formats.
    /// </summary>
    [Fact]
    public void ChooseImageFormatFallsBackToSourceWhenNoSupportDeclared()
    {
        var result = PlaybackFormatSelector.ChooseImageFormat("tiff", Array.Empty<string>());

        result.Should().Be("tiff");
    }

    /// <summary>
    /// Uses metadata type to classify audio even when extension is unknown.
    /// </summary>
    [Fact]
    public void ResolveMediaTypeUsesMetadataForAudio()
    {
        var mediaType = PlaybackFormatSelector.ResolveMediaType(
            "dat",
            MetadataType.Track
        );

        mediaType.Should().Be("Audio");
    }

    /// <summary>
    /// Uses extension to classify as photo when metadata type is unknown.
    /// </summary>
    [Fact]
    public void ResolveMediaTypeUsesImageExtensionWhenMetadataUnknown()
    {
        var mediaType = PlaybackFormatSelector.ResolveMediaType(
            "png",
            MetadataType.Unknown
        );

        mediaType.Should().Be("Photo");
    }
}
