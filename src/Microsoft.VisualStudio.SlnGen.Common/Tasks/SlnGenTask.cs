// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

#if NETSTANDARD
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.SlnGen.ProjectLoading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.VisualStudio.SlnGen.Tasks
{
    public class SlnGenTask : Task
    {
        private static readonly IDictionary<string, string> GlobalProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string[] Args { get; set; }

        public string VisualStudioInstallDirectory { get; set; }

        public override bool Execute()
        {
            if (Args.Any(i => i.Equals("--debug", StringComparison.OrdinalIgnoreCase)))
            {
                Debugger.Launch();
            }

            return SharedProgram.Main(Args, Execute) == 0;
        }

        private int Execute(ProgramArguments arguments, IConsole console)
        {
            FileInfo msbuildExeFileInfo = new FileInfo(Path.Combine(Path.GetDirectoryName(typeof(ProjectCollection).Assembly.Location), "MSBuild.dll"));

            ISlnGenLogger logger = new TaskLogger(Log);

            ProjectCollection projectCollection = GetProjectCollection();

            if (!arguments.TryGetEntryProjectPaths(logger, out IReadOnlyList<string> projectEntryPaths))
            {
                return 1;
            }

            (TimeSpan evaluationTime, int evaluationCount) = ProjectLoader.LoadProjects(msbuildExeFileInfo, projectCollection, projectEntryPaths, arguments.GetGlobalProperties(), logger);

            if (logger.HasLoggedErrors)
            {
                return 1;
            }

            (string solutionFileFullPath, int customProjectTypeGuidCount, int solutionItemCount, Guid solutionGuid) = SlnFile.GenerateSolutionFile(arguments, projectCollection.LoadedProjects.Where(i => !i.GlobalProperties.ContainsKey("TargetFramework")), logger);

            VisualStudioInstance visualStudioInstance = VisualStudioConfiguration.GetInstanceForPath(VisualStudioInstallDirectory);

            if (!VisualStudioLauncher.TryLaunch(arguments, visualStudioInstance, solutionFileFullPath, logger))
            {
                return 1;
            }

            SharedProgram.LogTelemetry(arguments, evaluationTime, evaluationCount, customProjectTypeGuidCount, solutionItemCount, solutionGuid);

            return 0;
        }

        private ProjectCollection GetProjectCollection()
        {
            ProjectCollection projectCollection = new ProjectCollection(
                null,
                loggers: null,
                remoteLoggers: null,
                toolsetDefinitionLocations: ToolsetDefinitionLocations.Default,
                maxNodeCount: 1,
                onlyLogCriticalEvents: true,
                loadProjectsReadOnly: true);

            typeof(ProjectCollection).GetField("_loggingService", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(projectCollection, BuildEngine.GetLoggingService());

            return projectCollection;
        }
    }
}
#endif