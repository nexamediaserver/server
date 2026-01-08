// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System.Text.Json;

using FluentAssertions;

using NexaMediaServer.Core.Helpers;

using Xunit;

namespace NexaMediaServer.Tests.Unit;

/// <summary>
/// Tests for <see cref="ExtraFieldsAccessor"/> type coercion and null handling.
/// </summary>
public class ExtraFieldsAccessorTests
{
    #region GetString Tests

    [Fact]
    public void GetStringReturnsNullWhenDictionaryIsNull()
    {
        ExtraFieldsAccessor.GetString(null, "key").Should().BeNull();
    }

    [Fact]
    public void GetStringReturnsNullWhenKeyNotPresent()
    {
        var dict = new Dictionary<string, JsonElement>();
        ExtraFieldsAccessor.GetString(dict, "missing").Should().BeNull();
    }

    [Fact]
    public void GetStringReturnsValueWhenStringElement()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["key"] = JsonDocument.Parse("\"hello\"").RootElement,
        };
        ExtraFieldsAccessor.GetString(dict, "key").Should().Be("hello");
    }

    [Fact]
    public void GetStringReturnsNumberAsString()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["key"] = JsonDocument.Parse("42").RootElement,
        };
        ExtraFieldsAccessor.GetString(dict, "key").Should().Be("42");
    }

    [Fact]
    public void GetStringReturnsTrueAs1()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["key"] = JsonDocument.Parse("true").RootElement,
        };
        ExtraFieldsAccessor.GetString(dict, "key").Should().Be("1");
    }

    [Fact]
    public void GetStringReturnsFalseAs0()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["key"] = JsonDocument.Parse("false").RootElement,
        };
        ExtraFieldsAccessor.GetString(dict, "key").Should().Be("0");
    }

    [Fact]
    public void GetStringReturnsNullWhenNullElement()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["key"] = JsonDocument.Parse("null").RootElement,
        };
        ExtraFieldsAccessor.GetString(dict, "key").Should().BeNull();
    }

    #endregion

    #region GetInt Tests

    [Fact]
    public void GetIntReturnsNullWhenDictionaryIsNull()
    {
        ExtraFieldsAccessor.GetInt(null, "key").Should().BeNull();
    }

    [Fact]
    public void GetIntReturnsNullWhenKeyNotPresent()
    {
        var dict = new Dictionary<string, JsonElement>();
        ExtraFieldsAccessor.GetInt(dict, "missing").Should().BeNull();
    }

    [Fact]
    public void GetIntReturnsValueWhenNumberElement()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["key"] = JsonDocument.Parse("42").RootElement,
        };
        ExtraFieldsAccessor.GetInt(dict, "key").Should().Be(42);
    }

    [Fact]
    public void GetIntParsesStringAsInt()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["key"] = JsonDocument.Parse("\"123\"").RootElement,
        };
        ExtraFieldsAccessor.GetInt(dict, "key").Should().Be(123);
    }

    [Fact]
    public void GetIntReturnsNullWhenStringNotParseable()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["key"] = JsonDocument.Parse("\"hello\"").RootElement,
        };
        ExtraFieldsAccessor.GetInt(dict, "key").Should().BeNull();
    }

    #endregion

    #region GetBool Tests

    [Fact]
    public void GetBoolReturnsNullWhenDictionaryIsNull()
    {
        ExtraFieldsAccessor.GetBool(null, "key").Should().BeNull();
    }

    [Fact]
    public void GetBoolReturnsTrueWhenTrueElement()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["key"] = JsonDocument.Parse("true").RootElement,
        };
        ExtraFieldsAccessor.GetBool(dict, "key").Should().BeTrue();
    }

    [Fact]
    public void GetBoolReturnsFalseWhenFalseElement()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["key"] = JsonDocument.Parse("false").RootElement,
        };
        ExtraFieldsAccessor.GetBool(dict, "key").Should().BeFalse();
    }

    [Fact]
    public void GetBoolReturnsTrueWhenNumber1()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["key"] = JsonDocument.Parse("1").RootElement,
        };
        ExtraFieldsAccessor.GetBool(dict, "key").Should().BeTrue();
    }

    [Fact]
    public void GetBoolReturnsFalseWhenNumber0()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["key"] = JsonDocument.Parse("0").RootElement,
        };
        ExtraFieldsAccessor.GetBool(dict, "key").Should().BeFalse();
    }

    [Theory]
    [InlineData("\"true\"", true)]
    [InlineData("\"yes\"", true)]
    [InlineData("\"1\"", true)]
    [InlineData("\"false\"", false)]
    [InlineData("\"no\"", false)]
    [InlineData("\"0\"", false)]
    public void GetBoolParsesStrings(string json, bool expected)
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["key"] = JsonDocument.Parse(json).RootElement,
        };
        ExtraFieldsAccessor.GetBool(dict, "key").Should().Be(expected);
    }

    #endregion

    #region GetStringArray Tests

    [Fact]
    public void GetStringArrayReturnsNullWhenDictionaryIsNull()
    {
        ExtraFieldsAccessor.GetStringArray(null, "key").Should().BeNull();
    }

    [Fact]
    public void GetStringArrayReturnsArrayWhenArrayElement()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["key"] = JsonDocument.Parse("[\"a\", \"b\", \"c\"]").RootElement,
        };
        ExtraFieldsAccessor.GetStringArray(dict, "key").Should().BeEquivalentTo(["a", "b", "c"]);
    }

    [Fact]
    public void GetStringArrayFiltersNullAndEmpty()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["key"] = JsonDocument.Parse("[\"a\", null, \"\", \"b\"]").RootElement,
        };
        ExtraFieldsAccessor.GetStringArray(dict, "key").Should().BeEquivalentTo(["a", "b"]);
    }

    [Fact]
    public void GetStringArrayWrapsSingleString()
    {
        var dict = new Dictionary<string, JsonElement>
        {
            ["key"] = JsonDocument.Parse("\"single\"").RootElement,
        };
        ExtraFieldsAccessor.GetStringArray(dict, "key").Should().BeEquivalentTo(["single"]);
    }

    #endregion

    #region SetString Tests

    [Fact]
    public void SetStringAddsValueWhenNotNull()
    {
        var dict = new Dictionary<string, JsonElement>();
        ExtraFieldsAccessor.SetString(dict, "key", "value");
        dict.Should().ContainKey("key");
        dict["key"].GetString().Should().Be("value");
    }

    [Fact]
    public void SetStringDoesNotAddWhenNull()
    {
        var dict = new Dictionary<string, JsonElement>();
        ExtraFieldsAccessor.SetString(dict, "key", null);
        dict.Should().NotContainKey("key");
    }

    [Fact]
    public void SetStringDoesNotAddWhenEmpty()
    {
        var dict = new Dictionary<string, JsonElement>();
        ExtraFieldsAccessor.SetString(dict, "key", string.Empty);
        dict.Should().NotContainKey("key");
    }

    #endregion

    #region SetInt Tests

    [Fact]
    public void SetIntAddsValueWhenNotNull()
    {
        var dict = new Dictionary<string, JsonElement>();
        ExtraFieldsAccessor.SetInt(dict, "key", 42);
        dict.Should().ContainKey("key");
        dict["key"].GetInt32().Should().Be(42);
    }

    [Fact]
    public void SetIntDoesNotAddWhenNull()
    {
        var dict = new Dictionary<string, JsonElement>();
        ExtraFieldsAccessor.SetInt(dict, "key", null);
        dict.Should().NotContainKey("key");
    }

    #endregion

    #region SetBool Tests

    [Fact]
    public void SetBoolAddsTrueWhenTrue()
    {
        var dict = new Dictionary<string, JsonElement>();
        ExtraFieldsAccessor.SetBool(dict, "key", true);
        dict.Should().ContainKey("key");
        dict["key"].GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void SetBoolAddsFalseWhenFalse()
    {
        var dict = new Dictionary<string, JsonElement>();
        ExtraFieldsAccessor.SetBool(dict, "key", false);
        dict.Should().ContainKey("key");
        dict["key"].GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void SetBoolDoesNotAddWhenNull()
    {
        var dict = new Dictionary<string, JsonElement>();
        ExtraFieldsAccessor.SetBool(dict, "key", null);
        dict.Should().NotContainKey("key");
    }

    #endregion
}
