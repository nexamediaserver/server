// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using NexaMediaServer.Core.DTOs;

namespace NexaMediaServer.Core.Services;

/// <summary>
/// Provides secure enumeration of server-side filesystem roots and directories for selection workflows.
/// </summary>
public interface IFileSystemBrowserService
{
    /// <summary>
    /// Gets the list of available filesystem roots (drives, mounts, etc.).
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A read-only list of filesystem roots.</returns>
    Task<IReadOnlyList<FileSystemRootDto>> GetRootsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Lists the entries of a given directory path after validating it against allowed roots and security checks.
    /// </summary>
    /// <param name="path">The absolute path to list.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A directory listing payload.</returns>
    Task<DirectoryListingDto> GetDirectoryAsync(string path, CancellationToken cancellationToken);
}
