// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    internal class TestLogger : SlnGenLoggerBase
    {
        private const int DefaultListSize = 100;

        public List<string> ErrorMessages { get; } = new List<string>(DefaultListSize);

        public List<BuildErrorEventArgs> Errors { get; } = new List<BuildErrorEventArgs>(DefaultListSize);

        public List<string> HighImportanceMessages { get; } = new List<string>(DefaultListSize);

        public List<string> LowImportanceMessages { get; } = new List<string>(DefaultListSize);

        public List<string> NormalImportanceMessages { get; } = new List<string>(DefaultListSize);

        public List<Tuple<string, IDictionary<string, string>>> Telemetry { get; } = new List<Tuple<string, IDictionary<string, string>>>();

        public List<string> WarningMessages { get; set; } = new List<string>(DefaultListSize);

        public List<BuildWarningEventArgs> Warnings { get; } = new List<BuildWarningEventArgs>(DefaultListSize);

        public override void LogError(string message, string code = null, string file = null, int lineNumber = 0, int columnNumber = 0)
        {
            Errors?.Add(new BuildErrorEventArgs(null, code, file, lineNumber, columnNumber, 0, 0, message, null, null));

            ErrorMessages?.Add(message);

            base.LogError(message, code);
        }

        public override void LogMessageHigh(string message, params object[] args) => HighImportanceMessages.Add(string.Format(CultureInfo.CurrentCulture, message, args));

        public override void LogMessageLow(string message, params object[] args) => LowImportanceMessages.Add(string.Format(CultureInfo.CurrentCulture, message, args));

        public override void LogMessageNormal(string message, params object[] args) => NormalImportanceMessages.Add(string.Format(CultureInfo.CurrentCulture, message, args));

        public override void LogTelemetry(string eventName, IDictionary<string, string> properties) => Telemetry.Add(new Tuple<string, IDictionary<string, string>>(eventName, properties));

        public override void LogWarning(string message, string code = null, string file = null, int lineNumber = 0, int columnNumber = 0) => Warnings?.Add(new BuildWarningEventArgs(null, code, file, lineNumber, columnNumber, 0, 0, message, null, null));
    }
}