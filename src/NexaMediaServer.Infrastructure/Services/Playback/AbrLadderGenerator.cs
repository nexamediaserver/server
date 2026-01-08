// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs.Playback;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.Playback;

/// <summary>
/// Generates adaptive bitrate ladders for transcoded streams based on source resolution.
/// </summary>
public class AbrLadderGenerator : IAbrLadderGenerator
{
    /// <summary>
    /// Standard ABR presets ordered by height (descending).
    /// </summary>
    private static readonly AbrPreset[] StandardPresets =
    [
        new(2160, 25_000_000, 192_000, "4K"),
        new(1440, 16_000_000, 192_000, "1440p"),
        new(1080, 8_000_000, 192_000, "1080p"),
        new(720, 4_000_000, 128_000, "720p"),
        new(480, 2_000_000, 128_000, "480p"),
        new(360, 1_000_000, 96_000, "360p"),
        new(240, 500_000, 64_000, "240p"),
    ];

    /// <inheritdoc />
    public AbrLadder GenerateLadder(
        int sourceWidth,
        int sourceHeight,
        int? sourceBitrate,
        int maxAllowedBitrate,
        bool includeSource = true
    )
    {
        var ladder = new AbrLadder
        {
            SourceWidth = sourceWidth,
            SourceHeight = sourceHeight,
            SourceBitrate = sourceBitrate,
            MaxAllowedBitrate = maxAllowedBitrate,
            Variants = [],
        };

        // Filter presets to only include those at or below source resolution
        var applicablePresets = StandardPresets
            .Where(p => p.Height <= sourceHeight)
            .ToList();

        // If source is smaller than all presets, use the smallest preset
        if (applicablePresets.Count == 0 && StandardPresets.Length > 0)
        {
            applicablePresets.Add(StandardPresets[^1]);
        }

        // Add source quality variant if requested and source is larger than highest preset
        if (includeSource && sourceHeight > applicablePresets.FirstOrDefault()?.Height)
        {
            int effectiveSourceBitrate = sourceBitrate ?? EstimateBitrate(sourceHeight);
            int cappedSourceBitrate = Math.Min(effectiveSourceBitrate, maxAllowedBitrate);

            // Only add source if it's meaningfully different from the first preset
            if (applicablePresets.Count == 0 || sourceHeight > applicablePresets[0].Height * 1.1)
            {
                ladder.Variants.Add(new AbrVariant
                {
                    Id = "source",
                    Label = $"{sourceHeight}p (Original)",
                    Width = sourceWidth,
                    Height = sourceHeight,
                    VideoBitrate = cappedSourceBitrate,
                    AudioBitrate = 192_000,
                    AudioChannels = 2,
                    IsSource = true,
                });
            }
        }

        // Add applicable presets, capped at max bitrate
        foreach (var preset in applicablePresets)
        {
            int cappedVideoBitrate = Math.Min(preset.VideoBitrate, maxAllowedBitrate - preset.AudioBitrate);
            if (cappedVideoBitrate <= 0)
            {
                continue;
            }

            // Calculate width maintaining aspect ratio
            int width = CalculateWidth(sourceWidth, sourceHeight, preset.Height);

            ladder.Variants.Add(new AbrVariant
            {
                Id = preset.Label.ToLowerInvariant(),
                Label = preset.Label,
                Width = width,
                Height = preset.Height,
                VideoBitrate = cappedVideoBitrate,
                AudioBitrate = preset.AudioBitrate,
                AudioChannels = 2,
                IsSource = false,
            });
        }

        // Ensure we have at least one variant
        if (ladder.Variants.Count == 0)
        {
            int fallbackBitrate = Math.Min(1_000_000, maxAllowedBitrate - 128_000);
            ladder.Variants.Add(new AbrVariant
            {
                Id = "auto",
                Label = "Auto",
                Width = sourceWidth,
                Height = sourceHeight,
                VideoBitrate = Math.Max(500_000, fallbackBitrate),
                AudioBitrate = 128_000,
                AudioChannels = 2,
                IsSource = true,
            });
        }

        return ladder;
    }

    /// <summary>
    /// Calculates the width for a target height while maintaining aspect ratio.
    /// </summary>
    private static int CalculateWidth(int sourceWidth, int sourceHeight, int targetHeight)
    {
        double aspectRatio = (double)sourceWidth / sourceHeight;
        int width = (int)Math.Round(targetHeight * aspectRatio);

        // Ensure width is even (required for most video codecs)
        return width % 2 == 0 ? width : width - 1;
    }

    /// <summary>
    /// Estimates a reasonable bitrate for a given height when source bitrate is unknown.
    /// </summary>
    private static int EstimateBitrate(int height)
    {
        return height switch
        {
            >= 2160 => 25_000_000,
            >= 1440 => 16_000_000,
            >= 1080 => 8_000_000,
            >= 720 => 4_000_000,
            >= 480 => 2_000_000,
            >= 360 => 1_000_000,
            _ => 500_000,
        };
    }

    /// <summary>
    /// Represents a standard ABR quality preset.
    /// </summary>
    private sealed record AbrPreset(int Height, int VideoBitrate, int AudioBitrate, string Label);
}
