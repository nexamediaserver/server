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
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Sidecar parser {ParserName} finished for {SidecarPath} in {ElapsedMs}ms"
    )]
    private static partial void LogSidecarParserFinished(
        ILogger logger,
        string parserName,
        string sidecarPath,
        long elapsedMs
    );

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sidecar parsing failed for {SidecarPath}")]
    private static partial void LogSidecarParseFailed(
        ILogger logger,
        string sidecarPath,
        Exception ex
    );

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Embedded extractor {ExtractorName} finished for {MediaPath} in {ElapsedMs}ms"
    )]
    private static partial void LogEmbeddedExtractorFinished(
        ILogger logger,
        string extractorName,
        string mediaPath,
        long elapsedMs
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
