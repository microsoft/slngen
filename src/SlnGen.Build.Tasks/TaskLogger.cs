// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Framework;
using SlnGen.Common;
using System;
using System.Collections.Generic;

namespace SlnGen.Build.Tasks
{
    /// <summary>
    /// Represents an implementation of <see cref="ISlnGenLogger" /> for an MSBuild task.
    /// </summary>
    internal class TaskLogger : SlnGenLoggerBase
    {
        private readonly IBuildEngine _buildEngine;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskLogger"/> class.
        /// </summary>
        /// <param name="buildEngine">An <see cref="IBuildEngine" /> instance to use for logging.</param>
        public TaskLogger(IBuildEngine buildEngine)
        {
            _buildEngine = buildEngine;
        }

        /// <inheritdoc cref="ISlnGenLogger.LogError" />
        public override void LogError(string message, string code = null, string file = null, int lineNumber = 0, int columnNumber = 0)
        {
            _buildEngine.LogErrorEvent(new BuildErrorEventArgs(null, code, file, lineNumber, columnNumber, 0, 0, message, null, null));

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
#if NET472 || NETCOREAPP
            ((IBuildEngine5)_buildEngine)?.LogTelemetry(eventName, properties);
#endif
        }

        /// <inheritdoc cref="ISlnGenLogger.LogWarning" />
        public override void LogWarning(string message, string code = null) => _buildEngine.LogWarningEvent(new BuildWarningEventArgs(null, code, null, 0, 0, 0, 0, message, null, null));
    }
}