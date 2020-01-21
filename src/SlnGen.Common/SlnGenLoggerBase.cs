// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading;

namespace SlnGen.Common
{
    /// <summary>
    /// Represents a base class for <see cref="ISlnGenLogger" /> classes.
    /// </summary>
    public abstract class SlnGenLoggerBase : ISlnGenLogger
    {
        private int _hasLoggedErrors = 0;

        /// <inheritdoc />
        public bool HasLoggedErrors => _hasLoggedErrors != 0;

        /// <inheritdoc />
        public virtual void LogError(string message, string code = null)
        {
            Interlocked.Exchange(ref _hasLoggedErrors, 1);
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
        public abstract void LogWarning(string message, string code = null);
    }
}
