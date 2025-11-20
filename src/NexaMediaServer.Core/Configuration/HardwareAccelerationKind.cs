// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Core.Configuration;

/// <summary>
/// Hardware acceleration modes supported by the server configuration.
/// </summary>
public enum HardwareAccelerationKind
{
    /// <summary>
    /// No hardware acceleration is used.
    /// </summary>
    None = 0,

    /// <summary>
    /// VAAPI acceleration (Linux GPUs supporting VA-API).
    /// </summary>
    Vaapi,

    /// <summary>
    /// Intel Quick Sync Video.
    /// </summary>
    Qsv,

    /// <summary>
    /// NVIDIA NVENC/NVDEC.
    /// </summary>
    Nvenc,
}
