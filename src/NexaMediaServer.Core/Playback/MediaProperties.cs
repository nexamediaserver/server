// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Playback;

/// <summary>
/// A flat representation of media properties used for profile condition evaluation.
/// </summary>
public sealed class MediaProperties
{
    /// <summary>
    /// Gets or sets the container format (e.g., mkv, mp4).
    /// </summary>
    public string? Container { get; set; }

    /// <summary>
    /// Gets or sets the video codec (e.g., h264, hevc).
    /// </summary>
    public string? VideoCodec { get; set; }

    /// <summary>
    /// Gets or sets the video profile (e.g., High, Main10).
    /// </summary>
    public string? VideoProfile { get; set; }

    /// <summary>
    /// Gets or sets the video level.
    /// </summary>
    public string? VideoLevel { get; set; }

    /// <summary>
    /// Gets or sets the video bit depth.
    /// </summary>
    public int? VideoBitDepth { get; set; }

    /// <summary>
    /// Gets or sets the video width in pixels.
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Gets or sets the video height in pixels.
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Gets or sets the video bitrate in bits per second.
    /// </summary>
    public int? VideoBitrate { get; set; }

    /// <summary>
    /// Gets or sets the video frame rate.
    /// </summary>
    public double? VideoFramerate { get; set; }

    /// <summary>
    /// Gets or sets the number of reference frames.
    /// </summary>
    public int? RefFrames { get; set; }

    /// <summary>
    /// Gets or sets the video range type (SDR, HDR10, etc).
    /// </summary>
    public string? VideoRangeType { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the video is interlaced.
    /// </summary>
    public bool IsInterlaced { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the video is anamorphic.
    /// </summary>
    public bool IsAnamorphic { get; set; }

    /// <summary>
    /// Gets or sets the audio codec.
    /// </summary>
    public string? AudioCodec { get; set; }

    /// <summary>
    /// Gets or sets the audio profile.
    /// </summary>
    public string? AudioProfile { get; set; }

    /// <summary>
    /// Gets or sets the number of audio channels.
    /// </summary>
    public int? AudioChannels { get; set; }

    /// <summary>
    /// Gets or sets the audio sample rate in Hz.
    /// </summary>
    public int? AudioSampleRate { get; set; }

    /// <summary>
    /// Gets or sets the audio bit depth.
    /// </summary>
    public int? AudioBitDepth { get; set; }

    /// <summary>
    /// Gets or sets the audio bitrate in bits per second.
    /// </summary>
    public int? AudioBitrate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a secondary audio track.
    /// </summary>
    public bool IsSecondaryAudio { get; set; }

    /// <summary>
    /// Gets or sets the total bitrate in bits per second.
    /// </summary>
    public int? TotalBitrate { get; set; }

    /// <summary>
    /// Gets or sets the number of video streams.
    /// </summary>
    public int NumVideoStreams { get; set; }

    /// <summary>
    /// Gets or sets the number of audio streams.
    /// </summary>
    public int NumAudioStreams { get; set; }

    /// <summary>
    /// Creates media properties from a <see cref="MediaItem"/> and optionally selected streams.
    /// </summary>
    /// <param name="item">The media item.</param>
    /// <param name="videoStream">The selected video stream, if any.</param>
    /// <param name="audioStream">The selected audio stream, if any.</param>
    /// <returns>A populated <see cref="MediaProperties"/> instance.</returns>
    public static MediaProperties FromMediaItem(
        MediaItem item,
        MediaStreamEntity? videoStream = null,
        MediaStreamEntity? audioStream = null)
    {
        var props = new MediaProperties
        {
            Container = item.FileFormat,
            VideoCodec = item.VideoCodec,
            VideoProfile = item.VideoCodecProfile,
            VideoLevel = item.VideoCodecLevel,
            VideoBitDepth = item.VideoBitDepth,
            Width = item.VideoWidth,
            Height = item.VideoHeight,
            VideoBitrate = item.VideoBitrate,
            VideoFramerate = item.VideoFrameRate,
            IsInterlaced = item.VideoIsInterlaced ?? false,
            VideoRangeType = item.VideoDynamicRange ?? "SDR",
            NumVideoStreams = item.VideoCodec != null ? 1 : 0,
            NumAudioStreams = item.AudioTrackCount ?? item.AudioCodecs.Count,
        };

        // Calculate total bitrate from parts if available
        if (item.VideoBitrate.HasValue)
        {
            var audioBitrate = item.AudioBitrates.FirstOrDefault();
            props.TotalBitrate = item.VideoBitrate.Value + audioBitrate;
        }

        // Apply audio track info from either explicit stream or fallback to item
        if (audioStream != null)
        {
            props.AudioCodec = audioStream.Codec;
            props.AudioChannels = audioStream.Channels;
            props.AudioBitrate = audioStream.Bitrate;
            props.IsSecondaryAudio = !audioStream.IsDefault;
        }
        else if (item.AudioCodecs.Count > 0)
        {
            props.AudioCodec = item.AudioCodecs.FirstOrDefault();
            props.AudioChannels = item.AudioChannelCounts.FirstOrDefault();
            props.AudioBitrate = item.AudioBitrates.FirstOrDefault();
            props.AudioSampleRate = item.AudioSampleRates.FirstOrDefault();
            props.AudioBitDepth = item.AudioBitDepths.FirstOrDefault();
        }

        return props;
    }
}
