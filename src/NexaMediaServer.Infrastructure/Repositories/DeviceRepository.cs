// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NexaMediaServer.Core.Entities;
using NexaMediaServer.Core.Repositories;
using NexaMediaServer.Infrastructure.Data;

namespace NexaMediaServer.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IDeviceRepository"/>.
/// </summary>
public sealed class DeviceRepository : IDeviceRepository
{
    private readonly MediaServerContext context;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceRepository"/> class.
    /// </summary>
    /// <param name="context">Database context.</param>
    public DeviceRepository(MediaServerContext context)
    {
        this.context = context;
    }

    /// <inheritdoc />
    public Task<Device?> GetByIdentifierAsync(
        string userId,
        string identifier,
        CancellationToken cancellationToken = default
    )
    {
        return this.context.Devices.FirstOrDefaultAsync(
            device => device.UserId == userId && device.Identifier == identifier,
            cancellationToken
        );
    }

    /// <inheritdoc />
    public Task<Device?> GetByIdAsync(int deviceId, CancellationToken cancellationToken = default)
    {
        return this.context.Devices.FirstOrDefaultAsync(
            device => device.Id == deviceId,
            cancellationToken
        );
    }

    /// <inheritdoc />
    public async Task<Device> CreateAsync(
        Device device,
        CancellationToken cancellationToken = default
    )
    {
        this.context.Devices.Add(device);
        await this.context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return device;
    }

    /// <inheritdoc />
    public async Task<Device> UpdateAsync(
        Device device,
        CancellationToken cancellationToken = default
    )
    {
        this.context.Devices.Update(device);
        await this.context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return device;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Device>> GetByUserAsync(
        string userId,
        CancellationToken cancellationToken = default
    )
    {
        var devices = await this
            .context.Devices.AsNoTracking()
            .Where(device => device.UserId == userId)
            .OrderBy(device => device.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return devices;
    }
}
