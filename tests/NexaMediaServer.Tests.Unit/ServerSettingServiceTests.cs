// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CA1707 // Identifiers should not contain underscores

using System.Globalization;

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using NexaMediaServer.Core.Constants;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Infrastructure.Services;

using Xunit;

namespace NexaMediaServer.Tests.Unit;

public class ServerSettingServiceTests
{
    private readonly Mock<IServerSettingRepository> repositoryMock;
    private readonly Mock<IConfiguration> configurationMock;
    private readonly ServerSettingService service;

    public ServerSettingServiceTests()
    {
        this.repositoryMock = new Mock<IServerSettingRepository>();
        this.configurationMock = new Mock<IConfiguration>();
        this.service = new ServerSettingService(
            this.repositoryMock.Object,
            this.configurationMock.Object,
            NullLogger<ServerSettingService>.Instance);
    }

    [Fact]
    public async Task GetAsyncReturnsValueFromDatabaseWhenKeyExists()
    {
        // Arrange
        const string key = ServerSettingKeys.ServerName;
        const string storedValue = "My Media Server";
        this.repositoryMock
            .Setup(r => r.GetByKeyAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServerSetting { Key = key, Value = storedValue });

        // Act
        var result = await this.service.GetAsync(key, "Default Name");

        // Assert
        result.Should().Be(storedValue);
    }

    [Fact]
    public async Task GetAsyncReturnsConfigurationFallbackWhenDatabaseHasNoValue()
    {
        // Arrange
        const string key = ServerSettingKeys.ServerName;
        const string configValue = "Config Server Name";
        this.repositoryMock
            .Setup(r => r.GetByKeyAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServerSetting?)null);
        this.configurationMock
            .Setup(c => c[$"ServerDefaults:{key}"])
            .Returns(configValue);

        // Act
        var result = await this.service.GetAsync(key, "Default Name");

        // Assert
        result.Should().Be(configValue);
    }

    [Fact]
    public async Task GetAsyncReturnsDefaultValueWhenNoValueExists()
    {
        // Arrange
        const string key = ServerSettingKeys.ServerName;
        const string defaultValue = "Default Server Name";
        this.repositoryMock
            .Setup(r => r.GetByKeyAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServerSetting?)null);
        this.configurationMock
            .Setup(c => c[$"ServerDefaults:{key}"])
            .Returns((string?)null);

        // Act
        var result = await this.service.GetAsync(key, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public async Task GetAsyncDeserializesIntegerCorrectly()
    {
        // Arrange
        const string key = ServerSettingKeys.MaxStreamingBitrate;
        const int storedValue = 40_000_000;
        this.repositoryMock
            .Setup(r => r.GetByKeyAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServerSetting { Key = key, Value = storedValue.ToString(CultureInfo.InvariantCulture) });

        // Act
        var result = await this.service.GetAsync(key, ServerSettingDefaults.MaxStreamingBitrate);

        // Assert
        result.Should().Be(storedValue);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("false", false)]
    [InlineData("False", false)]
    public async Task GetAsyncDeserializesBooleanCorrectly(string storedValue, bool expected)
    {
        // Arrange
        const string key = ServerSettingKeys.PreferH265;
        this.repositoryMock
            .Setup(r => r.GetByKeyAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServerSetting { Key = key, Value = storedValue });

        // Act
        var result = await this.service.GetAsync(key, false);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task SetAsyncStoresStringValue()
    {
        // Arrange
        const string key = ServerSettingKeys.ServerName;
        const string value = "New Server Name";

        // Act
        await this.service.SetAsync(key, value);

        // Assert
        this.repositoryMock.Verify(
            r => r.UpsertAsync(key, value, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SetAsyncSerializesIntegerCorrectly()
    {
        // Arrange
        const string key = ServerSettingKeys.MaxStreamingBitrate;
        const int value = 50_000_000;

        // Act
        await this.service.SetAsync(key, value);

        // Assert
        this.repositoryMock.Verify(
            r => r.UpsertAsync(key, "50000000", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public async Task SetAsyncSerializesBooleanAsLowercase(bool value, string expected)
    {
        // Arrange
        const string key = ServerSettingKeys.PreferH265;

        // Act
        await this.service.SetAsync(key, value);

        // Assert
        this.repositoryMock.Verify(
            r => r.UpsertAsync(key, expected, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAllAsyncReturnsAllSettings()
    {
        // Arrange
        var settings = new List<ServerSetting>
        {
            new() { Key = "Key1", Value = "Value1" },
            new() { Key = "Key2", Value = "Value2" },
        };
        this.repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        // Act
        var result = await this.service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result["Key1"].Should().Be("Value1");
        result["Key2"].Should().Be("Value2");
    }

    [Fact]
    public async Task DeleteAsyncDelegatesToRepository()
    {
        // Arrange
        const string key = "TestKey";
        this.repositoryMock
            .Setup(r => r.DeleteAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await this.service.DeleteAsync(key);

        // Assert
        result.Should().BeTrue();
        this.repositoryMock.Verify(
            r => r.DeleteAsync(key, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAsyncReturnsDefaultOnDeserializationFailure()
    {
        // Arrange
        const string key = ServerSettingKeys.MaxStreamingBitrate;
        const string invalidValue = "not-an-integer";
        this.repositoryMock
            .Setup(r => r.GetByKeyAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ServerSetting { Key = key, Value = invalidValue });
        this.configurationMock
            .Setup(c => c[$"ServerDefaults:{key}"])
            .Returns((string?)null);

        // Act
        var result = await this.service.GetAsync(key, 12345);

        // Assert
        result.Should().Be(12345);
    }
}
