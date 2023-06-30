// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.SlnGen.Launcher;
using Shouldly;
using System;
using System.Diagnostics;
using Xunit;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    public sealed class VisualStudioLauncherTests : TestBase
    {
        [Fact]
        public void VisualStudioLaunchProcessWindows()
        {
            IEnvironmentProvider environmentProvider = new MockEnvironmentProvider();
            MockLogger logger = new MockLogger();
            ForwardingLogger forwardingLogger = new ForwardingLogger(environmentProvider, new[] { logger }, noWarn: true);

            string devEnvFullPath = "C:\\Program Files\\Microsoft Visual Studio\\devenv.exe";
            string solutionPath = GetTempFileName(".sln");

            CommandLineBuilder commandLineBuilder = new CommandLineBuilder();
            commandLineBuilder.AppendTextUnquoted(solutionPath);

            var launcher = new VisualStudioLauncherWindows(forwardingLogger, environmentProvider);
            Process launchprocess = launcher.FetchLaunchProcessInfo(devEnvFullPath, commandLineBuilder);

            launchprocess.StartInfo.FileName.ShouldBe(devEnvFullPath);
            launchprocess.StartInfo.Arguments.ShouldBe(solutionPath);
            launchprocess.StartInfo.UseShellExecute.ShouldBe(true);
        }

        [Fact]
        public void VisualStudioLaunchProcessMac()
        {
            IEnvironmentProvider environmentProvider = new MockEnvironmentProvider();
            MockLogger logger = new MockLogger();
            ForwardingLogger forwardingLogger = new ForwardingLogger(environmentProvider, new[] { logger }, noWarn: true);

            string devEnvFullPath = "/Applications/Visual Studio.app";
            string solutionPath = GetTempFileName(".sln");

            CommandLineBuilder commandLineBuilder = new CommandLineBuilder();
            commandLineBuilder.AppendTextUnquoted(solutionPath);

            var launcher = new VisualStudioLauncherMac(forwardingLogger);
            Process launchprocess = launcher.FetchLaunchProcessInfo(devEnvFullPath, commandLineBuilder);

            launchprocess.StartInfo.FileName.ShouldBe("/usr/bin/open");
            launchprocess.StartInfo.Arguments.ShouldBe(string.Format("-a \"{0}\" \"{1}\"", devEnvFullPath, solutionPath));
            launchprocess.StartInfo.UseShellExecute.ShouldBe(true);
        }
    }
}