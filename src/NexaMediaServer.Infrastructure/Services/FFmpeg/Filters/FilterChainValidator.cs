// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Logging;

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Services;

namespace NexaMediaServer.Infrastructure.Services.FFmpeg.Filters;

/// <summary>
/// Validates video filter chains for correctness before execution.
/// </summary>
public sealed partial class FilterChainValidator : IFilterChainValidator
{
    // Hardware-specific filter prefixes/names
    private static readonly string[] HardwareFilterPatterns =
    [
        "scale_cuda", "scale_vaapi", "scale_qsv", "scale_vt", "scale_opencl", "vpp_qsv",
        "tonemap_cuda", "tonemap_vaapi", "tonemap_opencl",
        "yadif_cuda", "deinterlace_vaapi", "deinterlace_qsv", "bwdif_opencl",
        "transpose_cuda", "transpose_vaapi", "transpose_opencl",
        "hwupload", "hwdownload", "hwmap"
    ];

    private readonly ILogger<FilterChainValidator> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilterChainValidator"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public FilterChainValidator(ILogger<FilterChainValidator> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc/>
    public FilterChainValidationResult Validate(string? filterChain, VideoFilterContext context)
    {
        if (string.IsNullOrWhiteSpace(filterChain))
        {
            return FilterChainValidationResult.Success();
        }

        var errors = new List<string>();
        var filters = filterChain.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Check for hwupload requirement when SW decoder → HW encoder/filters
        if (!context.IsHardwareDecoder && context.IsHardwareEncoder)
        {
            var hasHwFilters = HasHardwareFilters(filters);
            var hasHwUpload = ContainsFilter(filters, "hwupload");

            if (hasHwFilters && !hasHwUpload)
            {
                errors.Add("Hardware filters detected but no hwupload found. Software decoder frames must be uploaded to GPU before hardware filtering.");
            }
        }

        // Check for hwdownload requirement when HW decoder → SW encoder
        if (context.IsHardwareDecoder && !context.IsHardwareEncoder)
        {
            var hasHwDownload = ContainsFilter(filters, "hwdownload");

            if (!hasHwDownload)
            {
                errors.Add("Hardware decoder used with software encoder but no hwdownload found. Hardware frames must be downloaded from GPU before software encoding.");
            }
        }

        // Check hwupload/hwdownload ordering
        ValidateHardwareTransferOrdering(filters, errors);

        // Check for mixed hardware device types
        ValidateConsistentHardwareDeviceType(filters, errors);

        if (errors.Count > 0)
        {
            LogFilterChainValidationFailed(this.logger, string.Join("; ", errors));
            return FilterChainValidationResult.Failure(errors, requiresSoftwareFallback: true);
        }

        return FilterChainValidationResult.Success();
    }

    private static bool HasHardwareFilters(string[] filters)
    {
        return filters.Any(f =>
            HardwareFilterPatterns.Any(pattern =>
                f.StartsWith(pattern, StringComparison.OrdinalIgnoreCase) &&
                !f.StartsWith("hwupload", StringComparison.OrdinalIgnoreCase) &&
                !f.StartsWith("hwdownload", StringComparison.OrdinalIgnoreCase)));
    }

    private static bool ContainsFilter(string[] filters, string filterName)
    {
        return filters.Any(f => f.StartsWith(filterName, StringComparison.OrdinalIgnoreCase));
    }

    private static void ValidateHardwareTransferOrdering(
        string[] filters,
        List<string> errors)
    {
        var hwUploadIndex = Array.FindIndex(filters, f =>
            f.StartsWith("hwupload", StringComparison.OrdinalIgnoreCase));
        var hwDownloadIndex = Array.FindIndex(filters, f =>
            f.StartsWith("hwdownload", StringComparison.OrdinalIgnoreCase));

        // Find first HW-specific filter (excluding hwupload/hwdownload)
        var firstHwFilterIndex = Array.FindIndex(filters, f =>
            HardwareFilterPatterns.Any(pattern =>
                f.StartsWith(pattern, StringComparison.OrdinalIgnoreCase) &&
                !f.StartsWith("hwupload", StringComparison.OrdinalIgnoreCase) &&
                !f.StartsWith("hwdownload", StringComparison.OrdinalIgnoreCase)));

        // If we have hwupload and HW filters, hwupload must come before HW filters
        if (hwUploadIndex >= 0 && firstHwFilterIndex >= 0 && hwUploadIndex > firstHwFilterIndex)
        {
            errors.Add("hwupload must appear before hardware filters in the chain.");
        }

        // If we have hwdownload, it should come after HW filters (if any)
        if (hwDownloadIndex >= 0 && firstHwFilterIndex >= 0 && hwDownloadIndex < firstHwFilterIndex)
        {
            errors.Add("hwdownload should appear after hardware filters in the chain.");
        }
    }

    private static void ValidateConsistentHardwareDeviceType(
        string[] filters,
        List<string> errors)
    {
        // Extract device types from filters
        var deviceTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var filter in filters)
        {
            if (filter.Contains("cuda", StringComparison.OrdinalIgnoreCase))
            {
                deviceTypes.Add("cuda");
            }

            if (filter.Contains("vaapi", StringComparison.OrdinalIgnoreCase))
            {
                deviceTypes.Add("vaapi");
            }

            if (filter.Contains("qsv", StringComparison.OrdinalIgnoreCase))
            {
                deviceTypes.Add("qsv");
            }

            if (filter.Contains("opencl", StringComparison.OrdinalIgnoreCase))
            {
                deviceTypes.Add("opencl");
            }

            if (filter.Contains("_vt", StringComparison.OrdinalIgnoreCase))
            {
                deviceTypes.Add("videotoolbox");
            }
        }

        // OpenCL can be mixed with other types (used for cross-device operations)
        deviceTypes.Remove("opencl");

        // QSV and VAAPI are often used together on Linux
        if (deviceTypes.Contains("qsv") && deviceTypes.Contains("vaapi"))
        {
            deviceTypes.Remove("vaapi");
        }

        if (deviceTypes.Count > 1)
        {
            errors.Add($"Mixed hardware device types detected: {string.Join(", ", deviceTypes)}. Filter chain should use consistent hardware type.");
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Filter chain validation failed: {Errors}")]
    private static partial void LogFilterChainValidationFailed(ILogger logger, string errors);
}
