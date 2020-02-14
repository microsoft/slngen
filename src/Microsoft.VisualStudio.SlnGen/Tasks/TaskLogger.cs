// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.SlnGen.Tasks
{
    /// <summary>
    /// Represents an implementation of <see cref="ISlnGenLogger" /> for an MSBuild task.
    /// </summary>
    internal class TaskLogger : SlnGenLoggerBase
    {
        private readonly IBuildEngine _buildEngine;
        private readonly IBuildEngine5 _buildEngine5;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskLogger"/> class.
        /// </summary>
        /// <param name="buildEngine">An <see cref="IBuildEngine" /> instance to use for logging.</param>
        public TaskLogger(IBuildEngine buildEngine)
        {
            _buildEngine = buildEngine;

            _buildEngine5 = buildEngine as IBuildEngine5;
        }

        /// <inheritdoc cref="ISlnGenLogger.LogError" />
        public override void LogError(string message, string code = null, string file = null, int lineNumber = 0, int columnNumber = 0)
        {
            _buildEngine.LogErrorEvent(
                new BuildErrorEventArgs(
                    subcategory: null,
                    code: code,
                    file: file,
                    lineNumber: lineNumber,
                    columnNumber: columnNumber,
                    endLineNumber: 0,
                    endColumnNumber: 0,
                    message: message,
                    helpKeyword: null,
                    senderName: null));

            base.LogError(message, code);
        }

        /// <inheritdoc cref="ISlnGenLogger.LogMessageHigh" />
        public override void LogMessageHigh(string message, params object[] args) => _buildEngine.LogMessageEvent(new BuildMessageEventArgs(message, null, null, MessageImportance.High, DateTime.UtcNow, args));

        /// <inheritdoc cref="ISlnGenLogger.LogMessageLow" />
        public override void LogMessageLow(string message, params object[] args) => _buildEngine.LogMessageEvent(new BuildMessageEventArgs(message, null, null, MessageImportance.Low, DateTime.UtcNow, args));

        /// <inheritdoc cref="ISlnGenLogger.LogMessageNormal" />
        public override void LogMessageNormal(string message, params object[] args) => _buildEngine.LogMessageEvent(new BuildMessageEventArgs(message, null, null, MessageImportance.Normal, DateTime.UtcNow, args));

        public override void LogTelemetry(string eventName, IDictionary<string, string> properties)
        {
            _buildEngine5?.LogTelemetry(eventName, properties);
        }

        /// <inheritdoc cref="ISlnGenLogger.LogWarning" />
        public override void LogWarning(string message, string code = null, string file = null, int lineNumber = 0, int columnNumber = 0) =>
            _buildEngine.LogWarningEvent(
                new BuildWarningEventArgs(
                    subcategory: null,
                    code: code,
                    file: file,
                    lineNumber: lineNumber,
                    columnNumber: columnNumber,
                    endLineNumber: 0,
                    endColumnNumber: 0,
                    message: message,
                    helpKeyword: null,
                    senderName: null));
    }
}