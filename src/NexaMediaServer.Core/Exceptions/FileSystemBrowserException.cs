// SPDX-FileCopyrightText: 2025 Nexa Contributors <contact@nexa.ms>
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Runtime.Serialization;

namespace NexaMediaServer.Core.Exceptions;

/// <summary>
/// Represents errors that occur when browsing the server filesystem.
/// </summary>
[Serializable]
public class FileSystemBrowserException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemBrowserException"/> class.
    /// </summary>
    public FileSystemBrowserException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemBrowserException"/> class with a specific message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public FileSystemBrowserException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemBrowserException"/> class with a specific message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public FileSystemBrowserException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemBrowserException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The serialization info that holds the serialized object data.</param>
    /// <param name="context">The streaming context that supplies the contextual information.</param>
#pragma warning disable SYSLIB0051 // Formatter-based serialization is legacy but required for exceptions.
    protected FileSystemBrowserException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
#pragma warning restore SYSLIB0051
}
