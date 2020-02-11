// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.SlnGen
{
    /// <summary>
    /// Represents a logger.
    /// </summary>
    public interface ISlnGenLogger
    {
        /// <summary>
        /// Gets a value indicating whether any errors have been logged.
        /// </summary>
        bool HasLoggedErrors { get; }

        /// <summary>
        /// Logs an error.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="code">An optional error code.</param>
        /// <param name="file">An optional file path.</param>
        /// <param name="lineNumber">An optional line number.</param>
        /// <param name="columnNumber">An optional column number.</param>
        void LogError(string message, string code = null, string file = null, int lineNumber = 0, int columnNumber = 0);

        /// <summary>
        /// Logs a high importance message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Optional format arguments for the message.</param>
        void LogMessageHigh(string message, params object[] args);

        /// <summary>
        /// Logs a low importance message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Optional format arguments for the message.</param>
        void LogMessageLow(string message, params object[] args);

        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="args">Optional format arguments for the message.</param>
        void LogMessageNormal(string message, params object[] args);

        /// <summary>
        /// Logs telemetry.
        /// </summary>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="properties">The properties of the event.</param>
        void LogTelemetry(string eventName, IDictionary<string, string> properties);

        /// <summary>
        /// Logs a warning.
        /// </summary>
        /// <param name="message">The warning message.</param>
        /// <param name="code">An optional error code.</param>
        /// <param name="file">An optional file path.</param>
        /// <param name="lineNumber">An optional line number.</param>
        /// <param name="columnNumber">An optional column number.</param>
        void LogWarning(string message, string code = null, string file = null, int lineNumber = 0, int columnNumber = 0);
    }
}