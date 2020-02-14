// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

#if NETFRAMEWORK
using Microsoft.VisualStudio.Telemetry;
#endif
using System;

namespace Microsoft.VisualStudio.SlnGen
{
    public class TelemetryData : IDisposable
    {
        private const string EventName = "msbuild/core/slngen";

        public TelemetryData()
        {
#if NETFRAMEWORK
            TelemetryService.DefaultSession.IsOptedIn = true;
            TelemetryService.DefaultSession.Start();
            TelemetryService.DefaultSession.PostEvent("msbuild/core/start");
#endif
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryData"/> class.
        /// </summary>
        /// <param name="program">The <see cref="Program" /> arguments.</param>
        public TelemetryData(Program program)
            : this()
        {
            DevEnvFullPathSpecified = !program.DevEnvFullPath.IsNullOrWhiteSpace();
            EntryProjectCount = program.Projects?.Length ?? 0;
            Folders = program.Folders;
            IsCoreXT = Program.IsCoreXT;
            LaunchVisualStudio = program.LaunchVisualStudio;
            SolutionFileFullPathSpecified = !program.SolutionFileFullPath.IsNullOrWhiteSpace();
            UseBinaryLogger = program.BinaryLogger.HasValue;
            UseFileLogger = program.FileLoggerParameters.HasValue;
            UseShellExecute = program.UseShellExecute;
        }

        public int CustomProjectTypeGuidCount { get; set; }

        public bool DevEnvFullPathSpecified { get; set; }

        public int EntryProjectCount { get; set; }

        public bool Folders { get; set; }

        public bool IsCoreXT { get; set; }

        public bool LaunchVisualStudio { get; set; }

        public int ProjectEvaluationCount { get; set; }

        public long ProjectEvaluationMilliseconds { get; set; }

        public bool SolutionFileFullPathSpecified { get; set; }

        public int SolutionItemCount { get; set; }

        public bool UseBinaryLogger { get; set; }

        public bool UseFileLogger { get; set; }

        public bool UseShellExecute { get; set; }

        public void Dispose()
        {
#if NETFRAMEWORK
            TelemetryEvent telemetryEvent = new TelemetryEvent(EventName);

            telemetryEvent.Properties.Add("slngen.internal.customprojecttypeguidcount", this.CustomProjectTypeGuidCount);

            TelemetryService.DefaultSession.PostEvent(telemetryEvent);
            TelemetryService.DefaultSession.PostEvent("msbuild/core/complete");
            TelemetryService.DefaultSession.Dispose();
#endif
        }
    }
}