using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace SlnGen
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            SlnError.Init();

            SlnGenProgramArguments arguments = new SlnGenProgramArguments(args);

            if (arguments.ShouldShowUsageAndExit)
            {
                Log.Error(arguments.Usage);

                Log.Verbose("The available error condition checks are:");
                foreach (string name in SlnError.Names)
                {
                    Log.Verbose(name);
                }
                Log.Error();

                Log.Error(ProgramArgumentsStrings.SolutionItemsDescription);
                Log.Error();
                Log.Error(ProgramArgumentsStrings.CompanionFilesDescription);
                Log.Error();

                return (int)arguments.ExitCode;
            }

            Log.Verbose("Looking for the following error conditions:");
            foreach (SlnError.ErrorId id in SlnError.Enabled.Keys)
            {
                Log.Verbose($" {id}");
            }
            Log.Verbose();

            try
            {
                if (arguments.ValidateOnly)
                {
                    return (int)Validate(arguments);
                }

                return (int)Run(arguments);
            }
            catch (Exception e)
            {
                Log.Error(e);
                Log.Error(arguments.Usage);
                return (int)arguments.ExitCode;
            }
        }

        public static ProgramExitCode Run(SlnGenProgramArguments arguments)
        {
            ProjectClosure projectClosure = new ProjectClosure(arguments, false);

            if (arguments.Purge)
            {
                Log.Info("Deleting temporary SlnGen solution directories...");
                Log.Info($"Deleted {SlnGenProgramArguments.CleanupTempSolutionDirectories(arguments.TemporaryDirectory)} SlnGen solution directories.");
            }

            arguments.GuessDefaults();

            if (arguments.SlnFile == null)
            {
                Log.Error("Can't determine target solution name. Specify with -o");
                return ProgramExitCode.TooManyProjectsFoundError;
            }

            if (arguments.InitialProjects.Count == 0)
            {
                Log.Error("There are no project files in this directory.");
                return ProgramExitCode.NoProjectsFoundError;
            }

            ProgramExitCode result = projectClosure.AddEntriesToParseFiles(arguments.InitialProjects, arguments.Recurse);

            if (result != ProgramExitCode.Success)
            {
                return result;
            }

            bool allProjectsValid = projectClosure.ProcessProjectFiles();

            SlnError.PrintErrors();

            if (!allProjectsValid)
            {
                Log.Error("Unable to load all projects.");
                return ProgramExitCode.BadProjectGuidsError;
            }

            if (projectClosure.ActualProjects.Count == 0)
            {
                Log.Error("No projects to load.");
                return ProgramExitCode.NoProjectsFoundError;
            }

            if (arguments.IncludeReverse > 0)
            {
                string root = arguments.GetWorkspaceRoot();
                if (root == null)
                {
                    Log.Error("Unable to figure out workspace root.");
                    return ProgramExitCode.NoProjectsFoundError;
                }
                Log.Verbose("Loading entire tree project closure to find children ...");
                ProjectClosure allProjectsClosure = new ProjectClosure(arguments, true);
                ProgramExitCode tmp = allProjectsClosure.AddEntriesToParseFiles(root, false, null, 1);
                if (tmp != ProgramExitCode.Success)
                {
                    SlnError.PrintErrors();
                    return tmp;
                }
                allProjectsClosure.ProcessProjectFiles();

                for (int i = 0; i < arguments.IncludeReverse; i++)
                {
                    Log.Verbose("Looking for reverse dependencies {0}/{1}...", i + 1, arguments.IncludeReverse);
                    List<string> children = allProjectsClosure.GetChildProjects(projectClosure.ActualProjects.Where(p => p.Level <= arguments.IncludeReverseLevel).Select(p => p.ProjectPath.FullName)).ToList();

                    foreach (string c in children)
                    {
                        Log.Verbose("Found reverse dependency: {0}", c);
                        projectClosure.AddEntriesToParseFiles(c, false, null, 100);
                    }
                    if (children.Count == 0)
                    {
                        Log.Verbose("Found no reverse dependencies in iteration {2}.", i + 1);
                        break;
                    }
                    projectClosure.ProcessProjectFiles();
                }
            }

            Log.Verbose("Creating {0}.", arguments.SlnFile);
            projectClosure.CreateTempSlnFile(arguments.SlnFile, arguments.Nest, arguments.RelativePaths, arguments.VisualStudio);

            if (arguments.LaunchVisualStudio)
            {
                try
                {
                    Log.Info();
                    Log.Info($"Opening '{Path.GetFileNameWithoutExtension(arguments.SlnFile)}' with {projectClosure.ProjectsLoadedCount} project(s) in Visual Studio.");
                    Log.Info();

                    Log.Verbose($"Running: {arguments.DevenvExe} {arguments.DevenvArgs}");

                    using (Process proc = new Process())
                    {
                        proc.StartInfo.FileName = arguments.DevenvExe;
                        proc.StartInfo.Arguments = arguments.DevenvArgs;
                        proc.Start();
                    }
                }
                catch (Exception e) when (!e.IsFatal())
                {
                    Log.Error(e);
                }
            }
            else
            {
                Log.Info($"Created '{Path.GetFileNameWithoutExtension(arguments.SlnFile)}' with {projectClosure.ProjectsLoadedCount} project(s).");
            }

            return 0;
        }

        public static ProgramExitCode Validate(SlnGenProgramArguments arguments)
        {
            ProgramExitCode result = ProgramExitCode.Success;
            ProjectClosure guidCheck = new ProjectClosure(arguments, false);
            guidCheck.AddEntriesToParseFiles(arguments.InitialProjects, arguments.Recurse);

            guidCheck.ProcessProjectFiles();

            SlnError.PrintErrors();
            if (SlnError.Errors.Count > 0)
            {
                result = ProgramExitCode.ValidationError;
            }

            if (arguments.InitialProjects.Count < 2 ||
                !SlnError.Enabled.ContainsKey(SlnError.ErrorId.ProjectsOverlapped))
            {
                return result;
            }

            SlnError.Errors.Clear();
            SlnError.Enabled.Clear();
            SlnError.Enabled[SlnError.ErrorId.ProjectsOverlapped] = SlnError.ErrorId.ProjectsOverlapped;

            SortedDictionary<string, ProjectClosure> overlap = new SortedDictionary<string, ProjectClosure>();
            foreach (string project in arguments.InitialProjects)
            {
                ProjectClosure currentClosure = new ProjectClosure(arguments, false);
                currentClosure.AddEntriesToParseFiles(project, arguments.Recurse, null, 1);
                currentClosure.ProcessProjectFiles();

                foreach (string other in overlap.Keys)
                {
                    if (!currentClosure.ValidateNoOverlap(project, overlap[other], other))
                    {
                        result = ProgramExitCode.ValidationError;
                    }
                }

                overlap.Add(project, currentClosure);
            }

            SlnError.PrintErrors();
            return result;
        }
    }
}
