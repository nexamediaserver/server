// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using HotChocolate.Authorization;
using NexaMediaServer.API.Types.Metadata;
using NexaMediaServer.Core.Enums;
using NexaMediaServer.Infrastructure.Services.Agents;
using NexaMediaServer.Infrastructure.Services.Metadata;
using NexaMediaServer.Infrastructure.Services.Parts;

namespace NexaMediaServer.API.Types;

/// <summary>
/// GraphQL query operations for metadata agents.
/// </summary>
public static partial class Query
{
    /// <summary>
    /// Gets all available metadata agents, sidecar parsers, and embedded metadata extractors.
    /// Optionally filtered by library type.
    /// </summary>
    /// <param name="libraryType">Optional library type to filter agents that support it.</param>
    /// <param name="partsRegistry">The parts registry containing discovered agents.</param>
    /// <returns>A list of available metadata agents.</returns>
    [Authorize]
    public static IEnumerable<MetadataAgentInfo> GetAvailableMetadataAgents(
        LibraryType? libraryType,
        [Service] IPartsRegistry partsRegistry
    )
    {
        var agents = new List<MetadataAgentInfo>();

        // Add sidecar parsers
        foreach (var parser in partsRegistry.SidecarParsers)
        {
            if (
                libraryType.HasValue
                && parser.SupportedLibraryTypes.Count > 0
                && !parser.SupportedLibraryTypes.Contains(libraryType.Value)
            )
            {
                continue;
            }

            agents.Add(
                new MetadataAgentInfo
                {
                    Name = parser.Name,
                    DisplayName = parser.DisplayName,
                    Description = parser.Description,
                    Category = MetadataAgentCategory.Sidecar,
                    DefaultOrder = parser.Order,
                    SupportedLibraryTypes = parser.SupportedLibraryTypes,
                }
            );
        }

        // Add embedded metadata extractors
        foreach (var extractor in partsRegistry.EmbeddedMetadataExtractors)
        {
            if (
                libraryType.HasValue
                && extractor.SupportedLibraryTypes.Count > 0
                && !extractor.SupportedLibraryTypes.Contains(libraryType.Value)
            )
            {
                continue;
            }

            agents.Add(
                new MetadataAgentInfo
                {
                    Name = extractor.Name,
                    DisplayName = extractor.DisplayName,
                    Description = extractor.Description,
                    Category = MetadataAgentCategory.Embedded,
                    DefaultOrder = extractor.Order,
                    SupportedLibraryTypes = extractor.SupportedLibraryTypes,
                }
            );
        }

        // Add metadata agents (local and remote)
        foreach (var agent in partsRegistry.MetadataAgents)
        {
            if (
                libraryType.HasValue
                && agent.SupportedLibraryTypes.Count > 0
                && !agent.SupportedLibraryTypes.Contains(libraryType.Value)
            )
            {
                continue;
            }

            var category =
                agent is ILocalMetadataAgent
                    ? MetadataAgentCategory.Local
                    : MetadataAgentCategory.Remote;

            agents.Add(
                new MetadataAgentInfo
                {
                    Name = agent.Name,
                    DisplayName = agent.DisplayName,
                    Description = agent.Description,
                    Category = category,
                    DefaultOrder = agent.Order,
                    SupportedLibraryTypes = agent.SupportedLibraryTypes,
                }
            );
        }

        // Return sorted by default order
        return agents.OrderBy(a => a.DefaultOrder).ThenBy(a => a.Name);
    }
}
