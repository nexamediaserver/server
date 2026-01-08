// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

#pragma warning disable CA1707 // Test method names use underscores for readability
#pragma warning disable CS1591 // XML comments not required for test classes

using FluentAssertions;

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Services;
using NexaMediaServer.Infrastructure.Services.FFmpeg.Filters;

using Xunit;

namespace NexaMediaServer.Tests.Unit;

/// <summary>
/// Tests for video filter pipeline.
/// </summary>
public sealed class VideoFilterPipelineTests
{
    private readonly TestFfmpegCapabilities defaultCapabilities;

    public VideoFilterPipelineTests()
    {
        // Default: support all filters and encoders
        defaultCapabilities = new TestFfmpegCapabilities
        {
            AllFiltersSupported = true,
            AllEncodersSupported = true,
            AllHwAccelSupported = true
        };
    }

    [Fact]
    public void Build_WithNoApplicableFilters_ReturnsNull()
    {
        // Arrange
        var filters = new List<IVideoFilter>();
        var pipeline = new VideoFilterPipeline(filters);
        var context = CreateContext(HardwareAccelerationKind.None);

        // Act
        var result = pipeline.Build(context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Build_AppliesFiltersInCorrectOrder()
    {
        // Arrange
        var capabilities = new TestFfmpegCapabilities
        {
            SupportedFilterNames = new HashSet<string> { "setparams", "bwdif", "scale" }
        };
        var filters = new List<IVideoFilter>
        {
            new ScaleFilter(),        // Order 30
            new DeinterlaceFilter(),  // Order 10
            new ColorPropertiesFilter() // Order 5
        };
        var pipeline = new VideoFilterPipeline(filters);
        var context = CreateContext(
            HardwareAccelerationKind.None,
            isInterlaced: true,
            targetWidth: 1280,  // Different from source 1920x1080 to trigger scaling
            targetHeight: 720,
            capabilities: capabilities);

        // Act
        var result = pipeline.Build(context);

        // Assert
        result.Should().NotBeNull();
        // Color properties should come first, then deinterlace, then scale
        result.Should().StartWith("setparams=color_primaries=bt709");
        result.Should().Contain("bwdif"); // Implementation prefers bwdif over yadif
        result.Should().Contain("scale");
    }

    [Theory]
    [InlineData(HardwareAccelerationKind.Nvenc, "scale_cuda")]
    [InlineData(HardwareAccelerationKind.Qsv, "vpp_qsv")]
    [InlineData(HardwareAccelerationKind.Vaapi, "scale_vaapi")]
    [InlineData(HardwareAccelerationKind.None, "scale")]
    public void Build_UsesCorrectScaleFilter_ForHardwareType(
        HardwareAccelerationKind hwAccel,
        string expectedFilter)
    {
        // Arrange
        var filters = new List<IVideoFilter> { new ScaleFilter() };
        var pipeline = new VideoFilterPipeline(filters);
        var context = CreateContext(hwAccel, targetWidth: 1280, targetHeight: 720);

        // Act
        var result = pipeline.Build(context);

        // Assert
        result.Should().Contain(expectedFilter);
    }

    [Theory]
    [InlineData(HardwareAccelerationKind.Nvenc, "tonemap_cuda")]
    [InlineData(HardwareAccelerationKind.Vaapi, "tonemap")]
    [InlineData(HardwareAccelerationKind.None, "tonemapx")]
    public void Build_UsesCorrectTonemapFilter_ForHardwareType(
        HardwareAccelerationKind hwAccel,
        string expectedFilterPrefix)
    {
        // Arrange
        var capabilities = new TestFfmpegCapabilities
        {
            SupportedFilterNames = hwAccel switch
            {
                HardwareAccelerationKind.Nvenc => new HashSet<string> { "tonemap_cuda", "zscale", "tonemapx" },
                HardwareAccelerationKind.Vaapi => new HashSet<string> { "tonemap_vaapi", "tonemap_opencl", "zscale", "tonemapx" },
                _ => new HashSet<string> { "zscale", "tonemapx" }
            }
        };

        var filters = new List<IVideoFilter> { new TonemapFilter() };
        var pipeline = new VideoFilterPipeline(filters);
        var context = CreateContext(hwAccel, isHdr: true, enableToneMapping: true, capabilities: capabilities);

        // Act
        var result = pipeline.Build(context);

        // Assert
        result.Should().Contain(expectedFilterPrefix);
    }

    [Theory]
    [InlineData(HardwareAccelerationKind.Nvenc, "yadif_cuda")]
    [InlineData(HardwareAccelerationKind.Qsv, "deinterlace_qsv")]
    [InlineData(HardwareAccelerationKind.Vaapi, "deinterlace_vaapi")]
    [InlineData(HardwareAccelerationKind.None, "bwdif")]
    public void Build_UsesCorrectDeinterlaceFilter_ForHardwareType(
        HardwareAccelerationKind hwAccel,
        string expectedFilter)
    {
        // Arrange
        var capabilities = new TestFfmpegCapabilities
        {
            SupportedFilterNames = hwAccel switch
            {
                HardwareAccelerationKind.Nvenc => new HashSet<string> { "yadif_cuda", "yadif", "bwdif" },
                HardwareAccelerationKind.Qsv => new HashSet<string> { "deinterlace_qsv", "vpp_qsv", "yadif", "bwdif" },
                HardwareAccelerationKind.Vaapi => new HashSet<string> { "deinterlace_vaapi", "yadif", "bwdif" },
                _ => new HashSet<string> { "yadif", "bwdif" }
            }
        };

        var filters = new List<IVideoFilter> { new DeinterlaceFilter() };
        var pipeline = new VideoFilterPipeline(filters);
        var context = CreateContext(hwAccel, isInterlaced: true, capabilities: capabilities);

        // Act
        var result = pipeline.Build(context);

        // Assert
        result.Should().Contain(expectedFilter);
    }

    [Fact]
    public void Build_SkipsScaleFilter_WhenNoScalingNeeded()
    {
        // Arrange
        var filters = new List<IVideoFilter> { new ScaleFilter() };
        var pipeline = new VideoFilterPipeline(filters);
        var context = CreateContext(HardwareAccelerationKind.None); // No target dimensions

        // Act
        var result = pipeline.Build(context);

        // Assert
        result.Should().BeNull(); // No scale needed, no filters applied
    }

    [Fact]
    public void Build_SkipsTonemapFilter_WhenNotHdr()
    {
        // Arrange
        var filters = new List<IVideoFilter> { new TonemapFilter() };
        var pipeline = new VideoFilterPipeline(filters);
        var context = CreateContext(HardwareAccelerationKind.None, isHdr: false);

        // Act
        var result = pipeline.Build(context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Build_SkipsTonemapFilter_WhenTonemappingDisabled()
    {
        // Arrange
        var filters = new List<IVideoFilter> { new TonemapFilter() };
        var pipeline = new VideoFilterPipeline(filters);
        var context = CreateContext(
            HardwareAccelerationKind.None,
            isHdr: true,
            enableToneMapping: false);

        // Act
        var result = pipeline.Build(context);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Build_CompleteHdrPipeline_NvencWithScalingAndTonemap()
    {
        // Arrange
        var capabilities = new TestFfmpegCapabilities
        {
            SupportedFilterNames = new HashSet<string> { "setparams", "scale_cuda", "tonemap_cuda", "format" }
        };
        var filters = new List<IVideoFilter>
        {
            new ColorPropertiesFilter(),
            new ScaleFilter(),
            new TonemapFilter(),
            new FormatFilter()
        };
        var pipeline = new VideoFilterPipeline(filters);
        var context = CreateContext(
            HardwareAccelerationKind.Nvenc,
            isHdr: true,
            enableToneMapping: true,
            targetWidth: 1280,  // Different from source 1920x1080 to trigger scaling
            targetHeight: 720,
            isHardwareEncoder: false, // Software encoder needs format filter
            capabilities: capabilities);

        // Act
        var result = pipeline.Build(context);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("setparams=color_primaries=bt709"); // Color properties
        result.Should().Contain("scale_cuda"); // CUDA scaling
        result.Should().Contain("tonemap_cuda"); // CUDA tonemap
        result.Should().Contain("format=yuv420p"); // Format conversion
    }

    [Fact]
    public void Build_AddsHardwareUpload_WhenSoftwareDecoderWithHardwareEncoder()
    {
        // Arrange
        var capabilities = new TestFfmpegCapabilities
        {
            SupportedFilterNames = new HashSet<string> { "hwupload", "scale_cuda" }
        };
        var filters = new List<IVideoFilter>
        {
            new HardwareUploadFilter(),
            new ScaleFilter()
        };
        var pipeline = new VideoFilterPipeline(filters);
        var context = CreateContext(
            HardwareAccelerationKind.Nvenc,
            isHardwareDecoder: false,
            isHardwareEncoder: true,
            targetWidth: 1280,  // Different from source 1920x1080 to trigger scaling
            targetHeight: 720,
            capabilities: capabilities);

        // Act
        var result = pipeline.Build(context);

        // Assert
        result.Should().Contain("hwupload=derive_device=cuda");
        result.Should().Contain("scale_cuda");
    }

    [Fact]
    public void Build_SkipsHardwareUpload_WhenHardwareDecoderUsed()
    {
        // Arrange
        var capabilities = new TestFfmpegCapabilities
        {
            SupportedFilterNames = new HashSet<string> { "hwupload", "scale_cuda" }
        };
        var filters = new List<IVideoFilter>
        {
            new HardwareUploadFilter(),
            new ScaleFilter()
        };
        var pipeline = new VideoFilterPipeline(filters);
        var context = CreateContext(
            HardwareAccelerationKind.Nvenc,
            isHardwareDecoder: true,
            isHardwareEncoder: true,
            targetWidth: 1280,  // Different from source 1920x1080 to trigger scaling
            targetHeight: 720,
            capabilities: capabilities);

        // Act
        var result = pipeline.Build(context);

        // Assert
        result.Should().NotContain("hwupload");
        result.Should().Contain("scale_cuda");
    }

    [Theory]
    [InlineData(90, "transpose=1")]
    [InlineData(180, "transpose=1,transpose=1")]
    [InlineData(270, "transpose=0")]
    public void Build_HandlesRotation_Correctly(int rotation, string expectedFilter)
    {
        // Arrange
        var filters = new List<IVideoFilter> { new TransposeFilter() };
        var pipeline = new VideoFilterPipeline(filters);
        var context = CreateContext(HardwareAccelerationKind.None, rotation: rotation);

        // Act
        var result = pipeline.Build(context);

        // Assert
        result.Should().Contain(expectedFilter);
    }

    [Fact]
    public void Build_WithFallback_WhenHardwareFilterNotSupported()
    {
        // Arrange
        var capabilities = new TestFfmpegCapabilities
        {
            SupportedFilterNames = new HashSet<string> { "zscale", "scale" } // No scale_cuda
        };

        var filters = new List<IVideoFilter> { new ScaleFilter() };
        var pipeline = new VideoFilterPipeline(filters);
        var context = CreateContext(
            HardwareAccelerationKind.Nvenc,
            targetWidth: 1280,  // Different from source 1920x1080 to trigger scaling
            targetHeight: 720,
            capabilities: capabilities);

        // Act
        var result = pipeline.Build(context);

        // Assert
        result.Should().Contain("zscale"); // Falls back to software
        result.Should().NotContain("scale_cuda");
    }

    private VideoFilterContext CreateContext(
        HardwareAccelerationKind hwAccel,
        bool isInterlaced = false,
        bool isHdr = false,
        bool enableToneMapping = true,
        int? targetWidth = null,
        int? targetHeight = null,
        int rotation = 0,
        bool isHardwareDecoder = false,
        bool isHardwareEncoder = false,
        IFfmpegCapabilities? capabilities = null)
    {
        return new VideoFilterContext
        {
            HardwareAcceleration = hwAccel,
            Capabilities = capabilities ?? defaultCapabilities,
            SourceVideoCodec = "h264",
            TargetVideoCodec = "h264",
            SourceWidth = 1920,
            SourceHeight = 1080,
            TargetWidth = targetWidth,
            TargetHeight = targetHeight,
            IsInterlaced = isInterlaced,
            IsHdr = isHdr,
            EnableToneMapping = enableToneMapping,
            Rotation = rotation,
            RequiresSubtitleOverlay = false,
            SubtitlePath = null,
            IsHardwareDecoder = isHardwareDecoder,
            IsHardwareEncoder = isHardwareEncoder
        };
    }

    /// <summary>
    /// Test implementation of IFfmpegCapabilities for unit testing.
    /// </summary>
    private sealed class TestFfmpegCapabilities : IFfmpegCapabilities
    {
        public bool AllFiltersSupported { get; init; }
        public bool AllEncodersSupported { get; init; }
        public bool AllHwAccelSupported { get; init; }
        public bool AllDecodersSupported { get; init; }
        public HashSet<string>? SupportedFilterNames { get; init; }
        public HashSet<string>? SupportedEncoderNames { get; init; }
        public HashSet<string>? SupportedDecoderNames { get; init; }
        public HashSet<HardwareAccelerationKind>? SupportedHwAccelKinds { get; init; }

        public string Version => "test-version";
        public IReadOnlySet<HardwareAccelerationKind> SupportedHwAccel =>
            SupportedHwAccelKinds ?? new HashSet<HardwareAccelerationKind>();
        public IReadOnlySet<string> SupportedEncoders =>
            SupportedEncoderNames ?? new HashSet<string>();
        public IReadOnlySet<string> SupportedFilters =>
            SupportedFilterNames ?? new HashSet<string>();
        public IReadOnlySet<string> SupportedDecoders =>
            SupportedDecoderNames ?? new HashSet<string>();
        public HardwareAccelerationKind RecommendedAcceleration => HardwareAccelerationKind.None;
        public bool IsDetected => true;

        public bool SupportsEncoder(string encoderName) =>
            AllEncodersSupported || (SupportedEncoderNames?.Contains(encoderName) ?? false);

        public bool SupportsFilter(string filterName) =>
            AllFiltersSupported || (SupportedFilterNames?.Contains(filterName) ?? false);

        public bool SupportsHwAccel(HardwareAccelerationKind kind) =>
            AllHwAccelSupported || (SupportedHwAccelKinds?.Contains(kind) ?? false);

        public bool SupportsDecoder(string decoderName) =>
            AllDecodersSupported || (SupportedDecoderNames?.Contains(decoderName) ?? false);

        public bool IsHardwareDecoderAvailable(string codec, HardwareAccelerationKind kind) =>
            AllDecodersSupported || AllHwAccelSupported;
    }
}
