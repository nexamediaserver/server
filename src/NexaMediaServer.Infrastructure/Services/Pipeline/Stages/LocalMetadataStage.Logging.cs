// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using Microsoft.Extensions.Logging;

namespace NexaMediaServer.Infrastructure.Services.Pipeline.Stages;

/// <summary>
/// Logging helpers for <see cref="LocalMetadataStage"/>.
/// </summary>
public sealed partial class LocalMetadataStage
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Parsed sidecar {SidecarPath} via {Parser}")]
    private static partial void LogSidecarParsed(ILogger logger, string sidecarPath, string parser);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sidecar parsing failed for {SidecarPath}")]
    private static partial void LogSidecarParseFailed(
        ILogger logger,
        string sidecarPath,
        Exception ex
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Extracted embedded metadata from {MediaPath} via {Extractor}"
    )]
    private static partial void LogEmbeddedExtracted(
        ILogger logger,
        string mediaPath,
        string extractor
    );

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Embedded extraction failed for {MediaPath}"
    )]
    private static partial void LogEmbeddedExtractionFailed(
        ILogger logger,
        string mediaPath,
        Exception ex
    );
}
