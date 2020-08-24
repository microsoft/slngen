// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.SlnGen
{
    internal sealed class TaskLogger : ISlnGenLogger
    {
        private readonly TaskLoggingHelper _log;

        public TaskLogger(TaskLoggingHelper log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public bool HasLoggedErrors { get; private set; }

        public bool IsDiagnostic { get; } = false;

        public int NextProjectId { get; } = 0;

        public void LogError(string message, string code = null, string file = null, int lineNumber = 0, int columnNumber = 0)
        {
            HasLoggedErrors = true;

            _log.LogError(null, code, null, file, lineNumber, columnNumber, 0, 0, message);
        }

        public void LogEvent(BuildEventArgs eventArgs)
        {
            throw new NotSupportedException();
        }

        public void LogMessageHigh(string message, params object[] args)
        {
            _log.LogMessage(MessageImportance.High, message, args);
        }

        public void LogMessageLow(string message, params object[] args)
        {
            _log.LogMessage(MessageImportance.Low, message, args);
        }

        public void LogMessageNormal(string message, params object[] args)
        {
            _log.LogMessage(MessageImportance.Normal, message, args);
        }

        public void LogTelemetry(string eventName, IDictionary<string, string> properties)
        {
            _log.LogTelemetry(eventName, properties);
        }

        public void LogWarning(string message, string code = null, string file = null, int lineNumber = 0, int columnNumber = 0)
        {
            _log.LogWarning(null, code, null, file, lineNumber, columnNumber, 0, 0, message);
        }
    }
}