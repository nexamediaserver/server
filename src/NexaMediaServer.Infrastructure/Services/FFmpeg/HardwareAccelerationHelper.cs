// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Runtime.InteropServices;

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.FFmpeg;

/// <summary>
/// Builds FFmpeg hardware acceleration arguments and encoder selection.
/// </summary>
public sealed class HardwareAccelerationHelper
{
    private readonly IFfmpegCapabilities capabilities;

    /// <summary>
    /// Initializes a new instance of the <see cref="HardwareAccelerationHelper"/> class.
    /// </summary>
    /// <param name="capabilities">The FFmpeg capabilities.</param>
    public HardwareAccelerationHelper(IFfmpegCapabilities capabilities)
    {
        this.capabilities = capabilities;
    }

    /// <summary>
    /// Gets the hardware device initialization arguments.
    /// </summary>
    /// <param name="kind">The hardware acceleration type.</param>
    /// <returns>The -init_hw_device arguments, or empty if none needed.</returns>
    public static string GetHardwareDeviceArgs(HardwareAccelerationKind kind)
    {
        return kind switch
        {
            HardwareAccelerationKind.Vaapi => GetVaapiDeviceArgs(),
            HardwareAccelerationKind.Qsv => GetQsvDeviceArgs(),
            HardwareAccelerationKind.Nvenc => "-init_hw_device cuda=cu:0",
            HardwareAccelerationKind.Amf => "-init_hw_device d3d11va=dx:0",
            HardwareAccelerationKind.VideoToolbox => "-init_hw_device videotoolbox=vt",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Gets the filter device selection argument (used by hwupload) for the hardware device.
    /// </summary>
    /// <param name="kind">The hardware acceleration type.</param>
    /// <returns>The -filter_hw_device argument, or empty if none needed.</returns>
    public static string GetFilterHardwareDeviceArg(HardwareAccelerationKind kind)
    {
        var alias = GetHardwareDeviceAlias(kind);
        return string.IsNullOrEmpty(alias) ? string.Empty : $"-filter_hw_device {alias}";
    }

    /// <summary>
    /// Gets the hardware decoder arguments.
    /// </summary>
    /// <param name="kind">The hardware acceleration type.</param>
    /// <returns>The -hwaccel and related decoder arguments.</returns>
    public static string GetHardwareDecoderArgs(HardwareAccelerationKind kind)
    {
        return kind switch
        {
            HardwareAccelerationKind.Vaapi => "-hwaccel vaapi -hwaccel_output_format vaapi",
            HardwareAccelerationKind.Qsv => "-hwaccel qsv -hwaccel_output_format qsv",
            HardwareAccelerationKind.Nvenc => "-hwaccel cuda -hwaccel_output_format cuda",
            HardwareAccelerationKind.Amf => "-hwaccel d3d11va -hwaccel_output_format d3d11",
            HardwareAccelerationKind.VideoToolbox => "-hwaccel videotoolbox -hwaccel_output_format videotoolbox",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Selects the best encoder for the given codec and hardware acceleration.
    /// Falls back to software encoder if hardware encoder is not available.
    /// </summary>
    /// <param name="codec">The codec name (e.g., "h264", "hevc", "av1").</param>
    /// <param name="kind">The hardware acceleration type.</param>
    /// <returns>The encoder name to use.</returns>
    public string SelectEncoder(string codec, HardwareAccelerationKind kind)
    {
        // Try hardware encoder first
        var hwEncoder = GetHardwareEncoder(codec, kind);
        if (hwEncoder != null && this.capabilities.SupportsEncoder(hwEncoder))
        {
            return hwEncoder;
        }

        // Fallback to software encoder
        return GetSoftwareEncoder(codec);
    }

    /// <summary>
    /// Checks if hardware decoding is available for the given acceleration type.
    /// </summary>
    /// <param name="kind">The hardware acceleration type.</param>
    /// <returns>True if hardware decoding is supported.</returns>
    public bool SupportsHardwareDecoding(HardwareAccelerationKind kind)
    {
        return kind != HardwareAccelerationKind.None && this.capabilities.SupportsHwAccel(kind);
    }

    /// <summary>
    /// Checks if hardware encoding is available for the given codec and acceleration type.
    /// </summary>
    /// <param name="codec">The codec name.</param>
    /// <param name="kind">The hardware acceleration type.</param>
    /// <returns>True if hardware encoding is supported.</returns>
    public bool SupportsHardwareEncoding(string codec, HardwareAccelerationKind kind)
    {
        var encoder = GetHardwareEncoder(codec, kind);
        return encoder != null && this.capabilities.SupportsEncoder(encoder);
    }

    private static string? GetHardwareEncoder(string codec, HardwareAccelerationKind kind)
    {
        return kind switch
        {
            HardwareAccelerationKind.Vaapi => codec switch
            {
                "h264" => "h264_vaapi",
                "hevc" => "hevc_vaapi",
                "av1" => "av1_vaapi",
                "mjpeg" => "mjpeg_vaapi",
                _ => null
            },
            HardwareAccelerationKind.Qsv => codec switch
            {
                "h264" => "h264_qsv",
                "hevc" => "hevc_qsv",
                "av1" => "av1_qsv",
                "mjpeg" => "mjpeg_qsv",
                "vp9" => "vp9_qsv",
                _ => null
            },
            HardwareAccelerationKind.Nvenc => codec switch
            {
                "h264" => "h264_nvenc",
                "hevc" => "hevc_nvenc",
                "av1" => "av1_nvenc",
                _ => null
            },
            HardwareAccelerationKind.Amf => codec switch
            {
                "h264" => "h264_amf",
                "hevc" => "hevc_amf",
                "av1" => "av1_amf",
                _ => null
            },
            HardwareAccelerationKind.VideoToolbox => codec switch
            {
                "h264" => "h264_videotoolbox",
                "hevc" => "hevc_videotoolbox",
                _ => null
            },
            HardwareAccelerationKind.Rkmpp => codec switch
            {
                "h264" => "h264_rkmpp",
                "hevc" => "hevc_rkmpp",
                _ => null
            },
            HardwareAccelerationKind.V4L2M2M => codec switch
            {
                "h264" => "h264_v4l2m2m",
                "hevc" => "hevc_v4l2m2m",
                _ => null
            },
            _ => null
        };
    }

    private static string GetSoftwareEncoder(string codec)
    {
        return codec switch
        {
            "h264" => "libx264",
            "hevc" => "libx265",
            "av1" => "libaom-av1",
            "vp9" => "libvpx-vp9",
            "vp8" => "libvpx",
            "mjpeg" => "mjpeg",
            _ => "libx264" // Default fallback
        };
    }

    private static string GetVaapiDeviceArgs()
    {
        // Default VAAPI render node on Linux
        const string renderNode = "/dev/dri/renderD128";
        return $"-init_hw_device vaapi=va:{renderNode}";
    }

    private static string GetQsvDeviceArgs()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: Use D3D11 with QSV
            return "-init_hw_device qsv=qs:hw";
        }
        else
        {
            // Linux: Derive QSV from VAAPI
            const string renderNode = "/dev/dri/renderD128";
            return $"-init_hw_device vaapi=va:{renderNode} -init_hw_device qsv=qs@va";
        }
    }

    private static string? GetHardwareDeviceAlias(HardwareAccelerationKind kind)
    {
        return kind switch
        {
            HardwareAccelerationKind.Vaapi => "va",
            HardwareAccelerationKind.Qsv => "qs",
            HardwareAccelerationKind.Nvenc => "cu",
            HardwareAccelerationKind.Amf => "dx",
            HardwareAccelerationKind.VideoToolbox => "vt",
            HardwareAccelerationKind.Rkmpp => "rk",
            HardwareAccelerationKind.V4L2M2M => "v4l2m2m",
            _ => null
        };
    }
}
