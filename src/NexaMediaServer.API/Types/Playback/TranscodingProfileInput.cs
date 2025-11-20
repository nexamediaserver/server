// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using NexaMediaServer.Core.DTOs.Playback;

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL input describing an allowed transcoding target.
/// </summary>
public sealed class TranscodingProfileInput
{
    /// <summary>
    /// Gets or sets the media type (Video/Audio/Photo).
    /// </summary>
    public string Type { get; set; } = "Video";

    /// <summary>
    /// Gets or sets the container output.
    /// </summary>
    public string Container { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the playback context (Streaming/Static).
    /// </summary>
    public string Context { get; set; } = "Streaming";

    /// <summary>
    /// Gets or sets the delivery protocol (e.g., hls).
    /// </summary>
    public string Protocol { get; set; } = "hls";

    /// <summary>
    /// Gets or sets the preferred audio codec.
    /// </summary>
    public string? AudioCodec { get; set; }

    /// <summary>
    /// Gets or sets the preferred video codec.
    /// </summary>
    public string? VideoCodec { get; set; }

    /// <summary>
    /// Gets or sets the maximum audio channels the client expects.
    /// </summary>
    public string? MaxAudioChannels { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether timestamps should be preserved when possible.
    /// </summary>
    public bool? CopyTimestamps { get; set; }

    /// <summary>
    /// Gets or sets the conditions under which this profile applies.
    /// </summary>
    public List<ProfileConditionInput> ApplyConditions { get; set; } = [];

    /// <summary>
    /// Maps this input into a DTO representation.
    /// </summary>
    /// <returns>A <see cref="TranscodingProfile"/> built from this input.</returns>
    internal TranscodingProfile ToDto()
    {
        return new TranscodingProfile
        {
            Type = this.Type,
            Container = this.Container,
            Context = this.Context,
            Protocol = this.Protocol,
            AudioCodec = this.AudioCodec,
            VideoCodec = this.VideoCodec,
            MaxAudioChannels = this.MaxAudioChannels,
            CopyTimestamps = this.CopyTimestamps,
            ApplyConditions = this.ApplyConditions.Select(c => c.ToDto()).ToList(),
        };
    }
}
