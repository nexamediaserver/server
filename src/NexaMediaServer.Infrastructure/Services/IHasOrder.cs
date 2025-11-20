// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace NexaMediaServer.Infrastructure.Services;

/// <summary>
/// Indicates that an extensibility part (metadata agent, analyzer, provider, etc.)
/// exposes an explicit execution order value.
/// </summary>
/// <remarks>
/// Lower values execute first. Parts not implementing this interface default to order 0.
/// Use <see cref="Core.Enums.MetadataAgentPriority"/> enum values for standard categories.
/// </remarks>
public interface IHasOrder
{
    /// <summary>
    /// Gets the execution order. Lower values run first.
    /// </summary>
    /// <remarks>
    /// Standard priority values from <see cref="Core.Enums.MetadataAgentPriority"/>:
    /// <list type="bullet">
    ///   <item><description>Sidecar = 10</description></item>
    ///   <item><description>Embedded = 20</description></item>
    ///   <item><description>Local = 30</description></item>
    ///   <item><description>Remote = 50</description></item>
    ///   <item><description>Fallback = 90</description></item>
    /// </list>
    /// </remarks>
    int Order { get; }
}
