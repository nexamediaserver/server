// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Frozen;

namespace NexaMediaServer.Common;

/// <summary>
/// Centralized immutable collections of known media file extensions grouped by media type.
/// Extensions are stored with a leading dot and compared case-insensitively.
/// </summary>
public static class MediaFileExtensions
{
    /// <summary>Known audio (music / audiobook / general audio) file extensions.</summary>
    public static readonly FrozenSet<string> AudioExtensions = new[]
    {
        ".3gp",
        ".669",
        ".aa",
        ".aac",
        ".aax",
        ".ac3",
        ".act",
        ".adp",
        ".adplug",
        ".adx",
        ".afc",
        ".aif",
        ".aiff",
        ".alac",
        ".amf",
        ".amr",
        ".ape",
        ".ast",
        ".au",
        ".awb",
        ".cda",
        ".cue",
        ".dmf",
        ".dsf",
        ".dsm",
        ".dsp",
        ".dts",
        ".dvf",
        ".eac3",
        ".ec3",
        ".far",
        ".flac",
        ".gdm",
        ".gsm",
        ".gym",
        ".hps",
        ".imf",
        ".it",
        ".m15",
        ".m4a",
        ".m4b",
        ".mac",
        ".med",
        ".mka",
        ".mmf",
        ".mod",
        ".mogg",
        ".mp+",
        ".mp2",
        ".mp3",
        ".mpa",
        ".mpc",
        ".mpp",
        ".msv",
        ".nmf",
        ".nsf",
        ".nsv",
        ".oga",
        ".ogg",
        ".okt",
        ".opus",
        ".pls",
        ".ra",
        ".rf64",
        ".rm",
        ".s3m",
        ".sfx",
        ".shn",
        ".sid",
        ".stm",
        ".strm",
        ".ult",
        ".uni",
        ".vox",
        ".wav",
        ".wma",
        ".wv",
        ".xm",
        ".xsp",
        ".ymf",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Known book / document reader file extensions.</summary>
    public static readonly FrozenSet<string> BookExtensions = new[]
    {
        ".azw",
        ".azw3",
        ".azw4",
        ".epub",
        ".docx",
        ".fb2",
        ".htmlz",
        ".kepub",
        ".oeb",
        ".lit",
        ".lrf",
        ".mobi",
        ".pdf",
        ".pmlz",
        ".rb",
        ".rtf",
        ".snb",
        ".tcr",
        ".txt",
        ".txtz",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Known comic archive file extensions.</summary>
    public static readonly FrozenSet<string> ComicExtensions = new[]
    {
        ".cb7",
        ".cba",
        ".cbr",
        ".cbt",
        ".cbz",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Known game / ROM / disc image file extensions.</summary>
    public static readonly FrozenSet<string> GameExtensions = new[]
    {
        ".32x",
        ".3ds",
        ".3dz",
        ".7z",
        ".7zip",
        ".adf.gz",
        ".adf",
        ".adz",
        ".apk",
        ".app",
        ".bin",
        ".bz2",
        ".cas",
        ".ccd",
        ".cci",
        ".cdc",
        ".cdi",
        ".chd",
        ".cia",
        ".cso",
        ".cue",
        ".dms",
        ".dnsp",
        ".dsi",
        ".dxci",
        ".ecm",
        ".fds",
        ".gb",
        ".gba",
        ".gbc",
        ".gcm",
        ".gcz",
        ".gdi",
        ".gen",
        ".gg",
        ".gz",
        ".gzip",
        ".ids",
        ".img",
        ".ipa",
        ".ipf",
        ".iso",
        ".isz",
        ".j64",
        ".jag",
        ".lnx",
        ".lyx",
        ".md",
        ".mdf",
        ".mds",
        ".mmi",
        ".n64",
        ".ndd",
        ".nds",
        ".nes",
        ".nez",
        ".ngc",
        ".ngp",
        ".nrg",
        ".nsp",
        ".obb",
        ".pce",
        ".rar",
        ".rvz",
        ".sbi",
        ".sfc",
        ".sgx",
        ".smc",
        ".smd",
        ".sms",
        ".srl",
        ".sub",
        ".tap",
        ".tar.bz2",
        ".tar.gz",
        ".tar",
        ".tzx",
        ".unf",
        ".unif",
        ".v64",
        ".vb",
        ".vpk",
        ".wad",
        ".wav",
        ".wbfs",
        ".ws",
        ".wsc",
        ".xci",
        ".xiso",
        ".z64",
        ".zcci",
        ".zcia",
        ".zip",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Known static image file extensions.</summary>
    public static readonly FrozenSet<string> ImageExtensions = new[]
    {
        ".avif",
        ".bmp",
        ".exr",
        ".gif",
        ".hdr",
        ".heic",
        ".heif",
        ".j2k",
        ".jfif",
        ".jif",
        ".jp2",
        ".jpc",
        ".jpe",
        ".jpeg",
        ".jpf",
        ".jpg",
        ".jpx",
        ".jxl",
        ".pbm",
        ".pfm",
        ".pgm",
        ".pic",
        ".png",
        ".pnm",
        ".ppm",
        ".raw",
        ".tif",
        ".tiff",
        ".webp",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Known video file extensions.</summary>
    public static readonly FrozenSet<string> VideoExtensions = new[]
    {
        ".264",
        ".265",
        ".3g2",
        ".3gp",
        ".amv",
        ".asf",
        ".avi",
        ".divx",
        ".dvr-ms",
        ".f4v",
        ".flv",
        ".gxf",
        ".h264",
        ".h265",
        ".hevc",
        ".img",
        ".ismv",
        ".iso",
        ".ivf",
        ".m1v",
        ".m2t",
        ".m2ts",
        ".m2v",
        ".m4v",
        ".mjpg",
        ".mjpeg",
        ".mk3d",
        ".mkv",
        ".mov",
        ".mp4",
        ".mpg",
        ".mpeg",
        ".mts",
        ".mxf",
        ".nut",
        ".nuv",
        ".ogg",
        ".ogm",
        ".ogv",
        ".ogx",
        ".ps",
        ".rec",
        ".ts",
        ".rm",
        ".rmvb",
        ".vdr",
        ".vro",
        ".vob",
        ".webm",
        ".wmv",
        ".wtv",
        ".y4m",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a value indicating whether the extension is a known audio extension.
    /// </summary>
    /// <param name="extension">The file extension (with or without leading dot).</param>
    /// <returns><c>true</c> if recognized as audio; otherwise <c>false</c>.</returns>
    public static bool IsAudio(string extension) => AudioExtensions.Contains(Normalize(extension));

    /// <summary>
    /// Gets a value indicating whether the extension is a known video extension.
    /// </summary>
    /// <param name="extension">The file extension (with or without leading dot).</param>
    /// <returns><c>true</c> if recognized as video; otherwise <c>false</c>.</returns>
    public static bool IsVideo(string extension) => VideoExtensions.Contains(Normalize(extension));

    /// <summary>
    /// Gets a value indicating whether the extension is a known static image extension.
    /// </summary>
    /// <param name="extension">The file extension (with or without leading dot).</param>
    /// <returns><c>true</c> if recognized as image; otherwise <c>false</c>.</returns>
    public static bool IsImage(string extension) => ImageExtensions.Contains(Normalize(extension));

    /// <summary>
    /// Gets a value indicating whether the extension is a known book/document extension.
    /// </summary>
    /// <param name="extension">The file extension (with or without leading dot).</param>
    /// <returns><c>true</c> if recognized as book/document; otherwise <c>false</c>.</returns>
    public static bool IsBook(string extension) => BookExtensions.Contains(Normalize(extension));

    /// <summary>
    /// Gets a value indicating whether the extension is a known comic archive extension.
    /// </summary>
    /// <param name="extension">The file extension (with or without leading dot).</param>
    /// <returns><c>true</c> if recognized as comic archive; otherwise <c>false</c>.</returns>
    public static bool IsComic(string extension) => ComicExtensions.Contains(Normalize(extension));

    /// <summary>
    /// Gets a value indicating whether the extension is a known game/ROM/disc extension.
    /// </summary>
    /// <param name="extension">The file extension (with or without leading dot).</param>
    /// <returns><c>true</c> if recognized as game/ROM/disc; otherwise <c>false</c>.</returns>
    public static bool IsGame(string extension) => GameExtensions.Contains(Normalize(extension));

    private static string Normalize(string ext)
    {
        if (string.IsNullOrWhiteSpace(ext))
        {
            return string.Empty;
        }

        return ext.StartsWith('.') ? ext : "." + ext;
    }
}
