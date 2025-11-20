// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Reflection;
using FluentAssertions;
using NexaMediaServer.Core.DTOs.Metadata;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Infrastructure.Services.Analysis;
using NexaMediaServer.Infrastructure.Services.Images;
using NexaMediaServer.Infrastructure.Services.Parts;
using Xunit;

namespace NexaMediaServer.Tests.Unit;

/// <summary>
/// Tests covering the <see cref="PartsRegistry"/> behaviors.
/// </summary>
public class PartsRegistryTests
{
    /// <summary>
    /// Ensures that providers registered for base metadata types are available for derived types.
    /// </summary>
    [Fact]
    public void GetImageProvidersUsesBaseTypeRegistrations()
    {
        var registry = new PartsRegistry();
        var provider = new TestImageProvider();
        InvokeInternal(registry, "TryAddImageProvider", typeof(Video), provider);
        registry.Freeze();

        var providers = registry.GetImageProviders<Movie>();

        providers.Should().HaveCount(1);
        providers[0].Should().BeOfType<TestImageProvider>();
    }

    /// <summary>
    /// Ensures that analyzers registered for base metadata types are available for derived types.
    /// </summary>
    [Fact]
    public void GetFileAnalyzersUsesBaseTypeRegistrations()
    {
        var registry = new PartsRegistry();
        var analyzer = new TestFileAnalyzer();
        InvokeInternal(registry, "TryAddFileAnalyzer", typeof(Video), analyzer);
        registry.Freeze();

        var analyzers = registry.GetFileAnalyzers<Movie>();

        analyzers.Should().HaveCount(1);
        analyzers[0].Should().BeOfType<TestFileAnalyzer>();
    }

    private static void InvokeInternal(
        PartsRegistry registry,
        string methodName,
        params object[] args
    )
    {
        var method =
            typeof(PartsRegistry).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic
            )
            ?? throw new InvalidOperationException(
                $"Method '{methodName}' not found on PartsRegistry."
            );
        method.Invoke(registry, args);
    }

    private sealed class TestImageProvider : IImageProvider<Video>
    {
        private readonly string name = "Test Provider";
        private readonly int order = 10;
        private int provideCalls;
        private int supportsCalls;

        public string Name => this.name;

        public int Order => this.order;

        public Task ProvideAsync(
            MediaItem item,
            Video metadata,
            IReadOnlyList<MediaPart> parts,
            CancellationToken cancellationToken
        )
        {
            this.provideCalls++;
            return Task.CompletedTask;
        }

        public bool Supports(MediaItem item, Video metadata)
        {
            this.supportsCalls++;
            return true;
        }
    }

    private sealed class TestFileAnalyzer : IFileAnalyzer<Video>
    {
        private readonly string name = "Test Analyzer";
        private readonly int order = 5;
        private int analyzeCalls;
        private int supportsCalls;

        public string Name => this.name;

        public int Order => this.order;

        public Task<FileAnalysisResult?> AnalyzeAsync(
            MediaItem item,
            Video metadata,
            IReadOnlyList<MediaPart> parts,
            CancellationToken cancellationToken
        )
        {
            this.analyzeCalls++;
            return Task.FromResult<FileAnalysisResult?>(null);
        }

        public bool Supports(MediaItem item, Video metadata)
        {
            this.supportsCalls++;
            return true;
        }
    }
}
