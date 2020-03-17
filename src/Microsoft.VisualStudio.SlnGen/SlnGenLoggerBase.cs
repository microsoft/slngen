// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Framework;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents a base class for <see cref="ISlnGenLogger" /> classes.
    /// </summary>
    public abstract class SlnGenLoggerBase : ISlnGenLogger
    {
        private int _hasLoggedErrors = 0;
        private int _projectId = 0;

        /// <inheritdoc />
        public bool HasLoggedErrors => _hasLoggedErrors != 0;

        /// <inheritdoc />
        public bool IsDiagnostic { get; set; }

        /// <inheritdoc />
        public int NextProjectId => Interlocked.Increment(ref _projectId);

        /// <inheritdoc />
        public virtual void LogError(string message, string code = null, string file = null, int lineNumber = 0, int columnNumber = 0)
        {
            Interlocked.Exchange(ref _hasLoggedErrors, 1);
        }

        /// <inheritdoc />
        public void LogEvent(BuildEventArgs eventArgs)
        {
            throw new System.NotImplementedException();
        }

        /// <inheritdoc />
        public abstract void LogMessageHigh(string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogMessageLow(string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogMessageNormal(string message, params object[] args);

        /// <inheritdoc />
        public abstract void LogTelemetry(string eventName, IDictionary<string, string> properties);

        /// <inheritdoc />
        public abstract void LogWarning(string message, string code = null, string file = null, int lineNumber = 0, int columnNumber = 0);
    }
}