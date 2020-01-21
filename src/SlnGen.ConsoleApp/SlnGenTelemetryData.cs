// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

#if NETFRAMEWORK
using Microsoft.VisualStudio.Telemetry;
#endif
using System;

namespace SlnGen.ConsoleApp
{
    public class SlnGenTelemetryData : IDisposable
    {
        private const string EventName = "msbuild/core/slngen";

        public SlnGenTelemetryData()
        {
#if NETFRAMEWORK
            TelemetryService.DefaultSession.IsOptedIn = true;
            TelemetryService.DefaultSession.Start();
            TelemetryService.DefaultSession.PostEvent("msbuild/core/start");
#endif
        }

        public int CustomProjectTypeGuidCount { get; set; }

        public bool DevEnvFullPathSpecified { get; set; }

        public int EntryProjectCount { get; set; }

        public bool Folders { get; set; }

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