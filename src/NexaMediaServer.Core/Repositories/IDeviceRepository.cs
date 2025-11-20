// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexaMediaServer.Core.Entities;

namespace NexaMediaServer.Core.Repositories;

/// <summary>
/// Repository contract for managing device entities.
/// </summary>
public interface IDeviceRepository
{
    /// <summary>
    /// Gets a device for the specified user/identifier pair, if it exists.
    /// </summary>
    /// <param name="userId">Identity user identifier.</param>
    /// <param name="identifier">Client-provided device identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching device, if present.</returns>
    Task<Device?> GetByIdentifierAsync(
        string userId,
        string identifier,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Gets a device by its primary key.
    /// </summary>
    /// <param name="deviceId">Database identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The device when found; otherwise null.</returns>
    Task<Device?> GetByIdAsync(int deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a newly created device.
    /// </summary>
    /// <param name="device">Device to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted entity.</returns>
    Task<Device> CreateAsync(Device device, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists changes to an existing device.
    /// </summary>
    /// <param name="device">Device to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated entity.</returns>
    Task<Device> UpdateAsync(Device device, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all devices registered by a user.
    /// </summary>
    /// <param name="userId">Identity user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only collection of devices.</returns>
    Task<IReadOnlyList<Device>> GetByUserAsync(
        string userId,
        CancellationToken cancellationToken = default
    );
}
