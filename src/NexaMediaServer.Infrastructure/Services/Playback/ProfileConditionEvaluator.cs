// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

using NexaMediaServer.Core.DTOs.Playback;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Core.Playback;

namespace NexaMediaServer.Infrastructure.Services.Playback;

/// <summary>
/// Evaluates profile conditions against media properties to determine playback compatibility.
/// </summary>
public sealed partial class ProfileConditionEvaluator : IProfileConditionEvaluator
{
    private readonly ILogger<ProfileConditionEvaluator> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileConditionEvaluator"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ProfileConditionEvaluator(ILogger<ProfileConditionEvaluator> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public ProfileEvaluationResult EvaluateConditions(
        MediaProperties properties,
        PlaybackCapabilities capabilities,
        string mediaType,
        bool forTranscoding = false)
    {
        var failedReasons = TranscodeReason.None;
        var failedConditions = new List<string>();

        // Evaluate codec profiles
        foreach (var codecProfile in capabilities.CodecProfiles)
        {
            if (!MatchesMediaType(codecProfile.Type, mediaType))
            {
                continue;
            }

            // Check if this profile applies to the current codecs
            if (!string.IsNullOrEmpty(codecProfile.Codec))
            {
                bool matchesVideo = MatchesCsv(properties.VideoCodec, codecProfile.Codec);
                bool matchesAudio = MatchesCsv(properties.AudioCodec, codecProfile.Codec);

                if (!matchesVideo && !matchesAudio)
                {
                    continue;
                }
            }

            if (!string.IsNullOrEmpty(codecProfile.Container) && !MatchesCsv(properties.Container, codecProfile.Container))
            {
                continue;
            }

            foreach (var condition in codecProfile.Conditions)
            {
                if (forTranscoding && !condition.IsRequiredForTranscoding)
                {
                    continue;
                }

                if (!forTranscoding && !condition.IsRequired)
                {
                    continue;
                }

                if (!this.EvaluateCondition(condition, properties, out var reason, out var description))
                {
                    failedReasons |= reason;
                    failedConditions.Add(description);
                }
            }
        }

        // Evaluate container profiles
        foreach (var containerProfile in capabilities.ContainerProfiles)
        {
            if (!MatchesMediaType(containerProfile.Type, mediaType))
            {
                continue;
            }

            foreach (var condition in containerProfile.Conditions)
            {
                if (forTranscoding && !condition.IsRequiredForTranscoding)
                {
                    continue;
                }

                if (!forTranscoding && !condition.IsRequired)
                {
                    continue;
                }

                if (!this.EvaluateCondition(condition, properties, out var reason, out var description))
                {
                    failedReasons |= reason;
                    failedConditions.Add(description);
                }
            }
        }

        if (failedReasons == TranscodeReason.None)
        {
            return ProfileEvaluationResult.Success;
        }

        LogConditionsFailed(
            this.logger,
            failedConditions.Count,
            string.Join(", ", failedConditions));

        return ProfileEvaluationResult.Failure(failedReasons, failedConditions);
    }

    /// <inheritdoc />
    public bool SupportsContainer(
        string? container,
        PlaybackCapabilities capabilities,
        string mediaType)
    {
        if (string.IsNullOrEmpty(container))
        {
            return false;
        }

        foreach (var profile in capabilities.DirectPlayProfiles)
        {
            if (!MatchesMediaType(profile.Type, mediaType))
            {
                continue;
            }

            if (MatchesCsv(container, profile.Container))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public bool SupportsVideoCodec(string? codec, DirectPlayProfile profile)
    {
        if (string.IsNullOrEmpty(profile.VideoCodec))
        {
            return true;
        }

        return MatchesCsv(codec, profile.VideoCodec);
    }

    /// <inheritdoc />
    public bool SupportsAudioCodec(string? codec, DirectPlayProfile profile)
    {
        if (string.IsNullOrEmpty(profile.AudioCodec))
        {
            return true;
        }

        return MatchesCsv(codec, profile.AudioCodec);
    }

    /// <inheritdoc />
    public TranscodingProfile? SelectTranscodingProfile(
        MediaProperties properties,
        PlaybackCapabilities capabilities,
        string mediaType)
    {
        foreach (var profile in capabilities.TranscodingProfiles)
        {
            if (!MatchesMediaType(profile.Type, mediaType))
            {
                continue;
            }

            // Check apply conditions
            bool allConditionsMet = true;
            foreach (var condition in profile.ApplyConditions)
            {
                if (!this.EvaluateCondition(condition, properties, out _, out _))
                {
                    allConditionsMet = false;
                    break;
                }
            }

            if (allConditionsMet)
            {
                return profile;
            }
        }

        // Return first compatible profile without conditions
        return capabilities.TranscodingProfiles
            .FirstOrDefault(p =>
                MatchesMediaType(p.Type, mediaType) &&
                p.ApplyConditions.Count == 0);
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Condition evaluation: {Property} {Condition} {ConditionValue} (actual: {ActualValue}) = {Result}")]
    private static partial void LogConditionEvaluation(
        ILogger logger,
        string property,
        string condition,
        string conditionValue,
        string actualValue,
        bool result);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "{Count} profile conditions failed: {Conditions}")]
    private static partial void LogConditionsFailed(
        ILogger logger,
        int count,
        string conditions);

    private static string? GetPropertyValue(string property, MediaProperties props)
    {
        return property switch
        {
            ProfileConditionProperty.AudioChannels => props.AudioChannels?.ToString(CultureInfo.InvariantCulture),
            ProfileConditionProperty.AudioCodec => props.AudioCodec,
            ProfileConditionProperty.AudioProfile => props.AudioProfile,
            ProfileConditionProperty.AudioSampleRate => props.AudioSampleRate?.ToString(CultureInfo.InvariantCulture),
            ProfileConditionProperty.AudioBitDepth => props.AudioBitDepth?.ToString(CultureInfo.InvariantCulture),
            ProfileConditionProperty.AudioBitrate => props.AudioBitrate?.ToString(CultureInfo.InvariantCulture),
            ProfileConditionProperty.VideoCodec => props.VideoCodec,
            ProfileConditionProperty.VideoProfile => props.VideoProfile,
            ProfileConditionProperty.VideoLevel => props.VideoLevel,
            ProfileConditionProperty.VideoBitDepth => props.VideoBitDepth?.ToString(CultureInfo.InvariantCulture),
            ProfileConditionProperty.Width => props.Width?.ToString(CultureInfo.InvariantCulture),
            ProfileConditionProperty.Height => props.Height?.ToString(CultureInfo.InvariantCulture),
            ProfileConditionProperty.VideoBitrate => props.VideoBitrate?.ToString(CultureInfo.InvariantCulture),
            ProfileConditionProperty.VideoFramerate => props.VideoFramerate?.ToString(CultureInfo.InvariantCulture),
            ProfileConditionProperty.RefFrames => props.RefFrames?.ToString(CultureInfo.InvariantCulture),
            ProfileConditionProperty.VideoRangeType => props.VideoRangeType,
            ProfileConditionProperty.IsInterlaced => props.IsInterlaced.ToString().ToLowerInvariant(),
            ProfileConditionProperty.IsAnamorphic => props.IsAnamorphic.ToString().ToLowerInvariant(),
            ProfileConditionProperty.IsSecondaryAudio => props.IsSecondaryAudio.ToString().ToLowerInvariant(),
            ProfileConditionProperty.Container => props.Container,
            ProfileConditionProperty.Bitrate => props.TotalBitrate?.ToString(CultureInfo.InvariantCulture),
            ProfileConditionProperty.NumVideoStreams => props.NumVideoStreams.ToString(CultureInfo.InvariantCulture),
            ProfileConditionProperty.NumAudioStreams => props.NumAudioStreams.ToString(CultureInfo.InvariantCulture),
            _ => null,
        };
    }

    private static TranscodeReason GetReasonForProperty(string property)
    {
        return property switch
        {
            ProfileConditionProperty.AudioChannels => TranscodeReason.AudioChannelsNotSupported,
            ProfileConditionProperty.AudioCodec => TranscodeReason.AudioCodecNotSupported,
            ProfileConditionProperty.AudioProfile => TranscodeReason.AudioProfileNotSupported,
            ProfileConditionProperty.AudioSampleRate => TranscodeReason.AudioSampleRateNotSupported,
            ProfileConditionProperty.AudioBitDepth => TranscodeReason.AudioBitDepthNotSupported,
            ProfileConditionProperty.AudioBitrate => TranscodeReason.AudioBitrateNotSupported,
            ProfileConditionProperty.VideoCodec => TranscodeReason.VideoCodecNotSupported,
            ProfileConditionProperty.VideoProfile => TranscodeReason.VideoProfileNotSupported,
            ProfileConditionProperty.VideoLevel => TranscodeReason.VideoLevelNotSupported,
            ProfileConditionProperty.VideoBitDepth => TranscodeReason.VideoBitDepthNotSupported,
            ProfileConditionProperty.Width or ProfileConditionProperty.Height => TranscodeReason.VideoResolutionNotSupported,
            ProfileConditionProperty.VideoBitrate => TranscodeReason.VideoBitrateNotSupported,
            ProfileConditionProperty.VideoFramerate => TranscodeReason.VideoFramerateNotSupported,
            ProfileConditionProperty.RefFrames => TranscodeReason.RefFramesNotSupported,
            ProfileConditionProperty.VideoRangeType => TranscodeReason.VideoRangeTypeNotSupported,
            ProfileConditionProperty.IsInterlaced => TranscodeReason.InterlacedVideoNotSupported,
            ProfileConditionProperty.Container => TranscodeReason.ContainerNotSupported,
            ProfileConditionProperty.Bitrate => TranscodeReason.VideoBitrateNotSupported,
            _ => TranscodeReason.Unknown,
        };
    }

    private static bool CompareEquals(string? value, string? target)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(target))
        {
            return string.IsNullOrEmpty(value) && string.IsNullOrEmpty(target);
        }

        return string.Equals(value, target, StringComparison.OrdinalIgnoreCase);
    }

    private static bool CompareLessThanEqual(string? value, string? target)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(target))
        {
            return true; // Missing value passes
        }

        if (double.TryParse(value, CultureInfo.InvariantCulture, out var numValue) &&
            double.TryParse(target, CultureInfo.InvariantCulture, out var numTarget))
        {
            return numValue <= numTarget;
        }

        return string.Compare(value, target, StringComparison.OrdinalIgnoreCase) <= 0;
    }

    private static bool CompareGreaterThanEqual(string? value, string? target)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(target))
        {
            return true; // Missing value passes
        }

        if (double.TryParse(value, CultureInfo.InvariantCulture, out var numValue) &&
            double.TryParse(target, CultureInfo.InvariantCulture, out var numTarget))
        {
            return numValue >= numTarget;
        }

        return string.Compare(value, target, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool CompareEqualsAny(string? value, string? csvList)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(csvList))
        {
            return false;
        }

        return MatchesCsv(value, csvList);
    }

    private static bool CompareMatches(string? value, string? pattern)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(pattern))
        {
            return string.IsNullOrEmpty(value);
        }

        try
        {
            return Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private static bool MatchesCsv(string? value, string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var tokens = csv.Split(
            ',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return tokens.Any(t => string.Equals(t, value, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesMediaType(string? profileType, string mediaType)
    {
        if (string.IsNullOrWhiteSpace(profileType))
        {
            return true;
        }

        return string.Equals(profileType, mediaType, StringComparison.OrdinalIgnoreCase);
    }

    private bool EvaluateCondition(
        ProfileCondition condition,
        MediaProperties properties,
        out TranscodeReason reason,
        out string description)
    {
        var propertyValue = GetPropertyValue(condition.Property, properties);
        var conditionValue = condition.Value;
        reason = GetReasonForProperty(condition.Property);

        bool result = condition.Condition switch
        {
            ProfileConditionType.Equal => CompareEquals(propertyValue, conditionValue),
            ProfileConditionType.NotEqual => !CompareEquals(propertyValue, conditionValue),
            ProfileConditionType.LessThanEqual => CompareLessThanEqual(propertyValue, conditionValue),
            ProfileConditionType.GreaterThanEqual => CompareGreaterThanEqual(propertyValue, conditionValue),
            ProfileConditionType.EqualsAny => CompareEqualsAny(propertyValue, conditionValue),
            ProfileConditionType.Matches => CompareMatches(propertyValue, conditionValue),
            _ => true, // Unknown condition type, pass by default
        };

        description = result
            ? string.Empty
            : $"{condition.Property} {condition.Condition} {conditionValue} (actual: {propertyValue ?? "null"})";

        if (!result)
        {
            LogConditionEvaluation(
                this.logger,
                condition.Property,
                condition.Condition,
                conditionValue,
                propertyValue ?? "null",
                result);
        }

        return result;
    }
}
