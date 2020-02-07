// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using SlnGen.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Build.Framework;

namespace SlnGen.UnitTests.Common
{
    public class TestLogger : SlnGenLoggerBase
    {
        public List<BuildErrorEventArgs> Errors { get; } = new List<BuildErrorEventArgs>();

        public List<string> ErrorMessages { get; } = new List<string>();

        public List<string> HighImportanceMessages { get; } = new List<string>();

        public List<string> LowImportanceMessages { get; } = new List<string>();

        public List<string> NormalImportanceMessages { get; } = new List<string>();

        public List<Tuple<string, IDictionary<string, string>>> Telemetry { get; } = new List<Tuple<string, IDictionary<string, string>>>();

        public List<Tuple<string, string>> Warnings { get; } = new List<Tuple<string, string>>();

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

        public override void LogWarning(string message, string code = null) => Warnings.Add(new Tuple<string, string>(message, code));
    }
}