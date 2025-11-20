// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using FluentAssertions;
using NexaMediaServer.Core.DTOs.Metadata;
using Xunit;

namespace NexaMediaServer.Tests.Unit;

/// <summary>
/// Tests ensuring <see cref="MetadataItemMapper"/> behaviors remain stable.
/// </summary>
public class MetadataItemMapperTests
{
    /// <summary>
    /// Verifies that mapping assigns a UUID when the DTO has not been populated yet.
    /// </summary>
    [Fact]
    public void MapToEntityAssignsUuidWhenDtoIsMissingOne()
    {
        var dto = new Movie
        {
            Uuid = Guid.Empty,
            Title = "Test",
            SortTitle = "Test",
            LibrarySectionId = 1,
        };

        var entity = MetadataItemMapper.MapToEntity(dto);

        entity.Uuid.Should().NotBe(Guid.Empty);
        dto.Uuid.Should().Be(entity.Uuid);
    }
}
