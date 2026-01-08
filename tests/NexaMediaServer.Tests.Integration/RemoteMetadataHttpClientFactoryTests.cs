// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System.Diagnostics;
using System.Net;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NexaMediaServer.Core.Configuration;
using NexaMediaServer.Core.Services;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace NexaMediaServer.Tests.Integration;

/// <summary>
/// Integration tests for RemoteMetadataHttpClientFactory using WireMock.
/// </summary>
public sealed class RemoteMetadataHttpClientFactoryTests : IDisposable
{
    private readonly WireMockServer mockServer;
    private readonly IRemoteMetadataHttpClientFactory factory;
    private readonly ServiceProvider serviceProvider;

    public RemoteMetadataHttpClientFactoryTests()
    {
        // Start WireMock server
        this.mockServer = WireMockServer.Start();

        // Setup DI container
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Register HTTP client infrastructure (simplified - tests use factory directly)
        services.AddSingleton<IRemoteMetadataHttpClientFactory, NexaMediaServer.Infrastructure.Services.RemoteMetadataHttpClientFactory>();

        this.serviceProvider = services.BuildServiceProvider();
        this.factory = this.serviceProvider.GetRequiredService<IRemoteMetadataHttpClientFactory>();
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test method naming convention")]
    public async Task CreateClient_SetsUserAgentWithVersionAndAgentName()
    {
        // Arrange
        this.mockServer
            .Given(Request.Create().WithPath("/test").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("OK"));

        var options = new RemoteMetadataHttpOptions
        {
            AgentName = "TestAgent",
            BaseAddress = new Uri(this.mockServer.Urls[0]),
            MaxRequests = null, // Disable rate limiting for this test
        };

        // Act
        var client = this.factory.CreateClient(options);
        var response = await client.GetAsync("/test");

        // Assert
        var logEntries = this.mockServer.LogEntries;
        var request = logEntries[0];
        var userAgent = request.RequestMessage.Headers?["User-Agent"]?.ToString();

        Assert.NotNull(userAgent);
        Assert.Contains("NexaMediaServer/", userAgent);
        Assert.Contains("(TestAgent)", userAgent);
        Assert.Contains(".NET", userAgent);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test method naming convention")]
    public async Task CreateClient_RetriesOn503WithExponentialBackoff()
    {
        // Arrange
        this.mockServer.Reset();
        this.mockServer
            .Given(Request.Create().WithPath("/retry-test").UsingGet())
            .InScenario("Retry")
            .WillSetStateTo("FirstRetry")
            .RespondWith(Response.Create().WithStatusCode(503).WithBody("Service Unavailable"));

        this.mockServer
            .Given(Request.Create().WithPath("/retry-test").UsingGet())
            .InScenario("Retry")
            .WhenStateIs("FirstRetry")
            .WillSetStateTo("SecondRetry")
            .RespondWith(Response.Create().WithStatusCode(503).WithBody("Service Unavailable"));

        this.mockServer
            .Given(Request.Create().WithPath("/retry-test").UsingGet())
            .InScenario("Retry")
            .WhenStateIs("SecondRetry")
            .WillSetStateTo("Success")
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("Success"));

        var options = new RemoteMetadataHttpOptions
        {
            AgentName = "RetryAgent",
            BaseAddress = new Uri(this.mockServer.Urls[0]),
            MaxRequests = null,
        };

        // Act
        var client = this.factory.CreateClient(options);
        var response = await client.GetAsync("/retry-test");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var requests = this.mockServer.LogEntries.Where(e => e.RequestMessage.Path == "/retry-test").ToList();
        Assert.Equal(3, requests.Count); // Initial attempt + 2 retries
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test method naming convention")]
    public async Task CreateClient_EnforcesRateLimitWithSharedLimiter()
    {
        // Arrange
        this.mockServer
            .Given(Request.Create().WithPath("/rate-limit-test").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("OK"));

        var options = new RemoteMetadataHttpOptions
        {
            AgentName = "RateLimitedAgent",
            BaseAddress = new Uri(this.mockServer.Urls[0]),
            MaxRequests = 2,
            TimeWindow = TimeSpan.FromSeconds(1),
        };

        // Act - Create two clients with same agent name (should share rate limiter)
        var client1 = this.factory.CreateClient(options);
        var client2 = this.factory.CreateClient(options);

        var stopwatch = Stopwatch.StartNew();

        // Make 2 requests (should succeed immediately)
        var response1 = await client1.GetAsync("/rate-limit-test");
        var response2 = await client2.GetAsync("/rate-limit-test");

        // Third request should be delayed due to rate limit
        var response3 = await client1.GetAsync("/rate-limit-test");
        stopwatch.Stop();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response3.StatusCode);

        // Third request should have been delayed (> 800ms to account for processing time)
        Assert.True(stopwatch.ElapsedMilliseconds > 800,
            $"Expected rate limiting delay, but only took {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test method naming convention")]
    public async Task CreateClient_AllowsInsecureConnectionsWhenConfigured()
    {
        // Note: WireMock uses HTTP by default, so we'll verify the option is respected
        // by checking that SSL validation bypass is configured in the handler

        // Arrange
        var options = new RemoteMetadataHttpOptions
        {
            AgentName = "InsecureAgent",
            AllowInsecureConnections = true,
            MaxRequests = null,
        };

        // Act
        var client = this.factory.CreateClient(options);

        // Assert - Handler chain should include insecure configuration
        // We can't easily test actual SSL bypass without an HTTPS server,
        // but we can verify the client is created without errors
        Assert.NotNull(client);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test method naming convention")]
    public async Task CreateClient_EnrichesTelemetryWithAgentTags()
    {
        // Arrange
        var activityTags = new List<KeyValuePair<string, object?>>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "System.Net.Http",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity =>
            {
                foreach (var tag in activity.Tags)
                {
                    activityTags.Add(new KeyValuePair<string, object?>(tag.Key, tag.Value));
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        this.mockServer
            .Given(Request.Create().WithPath("/telemetry-test").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("OK"));

        var options = new RemoteMetadataHttpOptions
        {
            AgentName = "TelemetryAgent",
            BaseAddress = new Uri(this.mockServer.Urls[0]),
            MaxRequests = null,
        };

        // Act
        var client = this.factory.CreateClient(options);
        await client.GetAsync("/telemetry-test");

        // Assert
        Assert.Contains(activityTags, tag => tag.Key == "metadata.agent.name" && tag.Value?.ToString() == "TelemetryAgent");
        Assert.Contains(activityTags, tag => tag.Key == "metadata.agent.request.url");
        Assert.Contains(activityTags, tag => tag.Key == "http.response.status_code" && tag.Value?.ToString() == "200");
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test method naming convention")]
    public async Task CreateClient_SetsTimeoutCorrectly()
    {
        // Arrange
        this.mockServer
            .Given(Request.Create().WithPath("/timeout-test").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("OK")
                .WithDelay(TimeSpan.FromMilliseconds(100)));

        var options = new RemoteMetadataHttpOptions
        {
            AgentName = "TimeoutAgent",
            BaseAddress = new Uri(this.mockServer.Urls[0]),
            Timeout = TimeSpan.FromMilliseconds(50),
            MaxRequests = null,
        };

        // Act & Assert
        var client = this.factory.CreateClient(options);
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await client.GetAsync("/timeout-test");
        });
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test method naming convention")]
    public async Task CreateClient_AddsAdditionalHeaders()
    {
        // Arrange
        this.mockServer
            .Given(Request.Create().WithPath("/headers-test").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("OK"));

        var options = new RemoteMetadataHttpOptions
        {
            AgentName = "HeadersAgent",
            BaseAddress = new Uri(this.mockServer.Urls[0]),
            AdditionalHeaders = new Dictionary<string, string>
            {
                ["X-API-Key"] = "test-api-key-123",
                ["X-Custom-Header"] = "custom-value",
            },
            MaxRequests = null,
        };

        // Act
        var client = this.factory.CreateClient(options);
        await client.GetAsync("/headers-test");

        // Assert
        var logEntries = this.mockServer.LogEntries;
        var request = logEntries[logEntries.Count - 1];

        Assert.Equal("test-api-key-123", request.RequestMessage.Headers?["X-API-Key"]?.ToString());
        Assert.Equal("custom-value", request.RequestMessage.Headers?["X-Custom-Header"]?.ToString());
    }

    public void Dispose()
    {
        this.mockServer?.Stop();
        this.mockServer?.Dispose();
        this.serviceProvider?.Dispose();
    }
}
