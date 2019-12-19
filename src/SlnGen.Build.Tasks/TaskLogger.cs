// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using Microsoft.Build.Framework;
using SlnGen.Common;
using System;

namespace SlnGen.Build.Tasks
{
    /// <summary>
    /// Represents an implementation of <see cref="ISlnGenLogger" /> for an MSBuild task.
    /// </summary>
    internal class TaskLogger : ISlnGenLogger
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
        public void LogError(string message, string code = null) => _buildEngine.LogErrorEvent(new BuildErrorEventArgs(null, code, null, 0, 0, 0, 0, message, null, null));

        /// <inheritdoc cref="ISlnGenLogger.LogMessageHigh" />
        public void LogMessageHigh(string message, params object[] args) => _buildEngine.LogMessageEvent(new BuildMessageEventArgs(message, null, null, MessageImportance.High, DateTime.UtcNow, args));

        /// <inheritdoc cref="ISlnGenLogger.LogMessageLow" />
        public void LogMessageLow(string message, params object[] args) => _buildEngine.LogMessageEvent(new BuildMessageEventArgs(message, null, null, MessageImportance.Low, DateTime.UtcNow, args));

        /// <inheritdoc cref="ISlnGenLogger.LogMessageNormal" />
        public void LogMessageNormal(string message, params object[] args) => _buildEngine.LogMessageEvent(new BuildMessageEventArgs(message, null, null, MessageImportance.Normal, DateTime.UtcNow, args));

        /// <inheritdoc cref="ISlnGenLogger.LogWarning" />
        public void LogWarning(string message, string code = null) => _buildEngine.LogWarningEvent(new BuildWarningEventArgs(null, code, null, 0, 0, 0, 0, message, null, null));
    }
}