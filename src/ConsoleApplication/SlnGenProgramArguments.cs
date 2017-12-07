using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SlnGen
{
    internal sealed class SlnGenProgramArguments : ProgramArguments
    {
        private const string TempSolutionDirPrefix = "sln_";

        private static readonly ProgramArgumentInfo[] Infos =
        {
            new ProgramArgumentInfo
            {
                Name = "files",
                IsPositional = true,
                Type = typeof(string[]),
                Description = SlnGenStrings.fileUsage,
                Default = new string[0],
            },
            new ProgramArgumentInfo
            {
                Name = "recurse",
                ShortForms = {"s"},
                Description = SlnGenStrings.recurseUsage,
                Default = false,
            },
            new ProgramArgumentInfo
            {
                Name = "hive",
                Description = SlnGenStrings.hiveUsage,
                Default = false,
            },
            new ProgramArgumentInfo
            {
                Name = "nest",
                ShortForms = {"n"},
                Description = SlnGenStrings.nestUsage,
                Default = false,
            },
            new ProgramArgumentInfo
            {
                Name = "noLaunch",
                ShortForms = {"no"},
                Description = SlnGenStrings.noLaunchUsage,
                Default = false,
            },
            new ProgramArgumentInfo
            {
                Name = "run",
                Type = typeof(string),
                Description = SlnGenStrings.runUsage,
                Default = null,
            },
            new ProgramArgumentInfo
            {
                Name = "outputFile",
                ShortForms = {"o"},
                Type = typeof(string),
                Description = SlnGenStrings.outputUsage,
                Default = null,
            },
            new ProgramArgumentInfo
            {
                Name = "includeReverse",
                ShortForms = {"ir"},
                Type = typeof(int),
                Description = SlnGenStrings.includeReverseUsage,
                Default = 0,
            },
            new ProgramArgumentInfo
            {
                Name = "includeReverseLevel",
                ShortForms = {"irl"},
                Type = typeof(int),
                Description = SlnGenStrings.includeReverseLevelUsage,
                Default = 1,
            },
            new ProgramArgumentInfo
            {
                Name = "useCurrent",
                ShortForms = {"w"},
                Description = SlnGenStrings.useCurrentUsage,
                Default = false,
            },
            new ProgramArgumentInfo
            {
                Name = "useWorkspace",
                ShortForms = {"k"},
                Description = SlnGenStrings.useWorkspaceUsage,
                Default = true,
            },
            new ProgramArgumentInfo
            {
                Name = "workspaceFiles",
                ShortForms = {"wf"},
                Description = SlnGenStrings.workspaceFilesUsage,
                Default = "build.root;sd.ini;nuget.config"
            },
            new ProgramArgumentInfo
            {
                Name = "workspaceSubDir",
                ShortForms = {"ws"},
                Description = SlnGenStrings.workspaceSubDir,
                Default = "sln"
            },
            new ProgramArgumentInfo
            {
                Name = "quiet",
                ShortForms = {"q"},
                Type = typeof(bool[]),
                Description = SlnGenStrings.quietUsage,
                Default = new bool[0],
            },
            new ProgramArgumentInfo
            {
                Name = "verbose",
                ShortForms = {"v"},
                Type = typeof(bool[]),
                Description = SlnGenStrings.verboseUsage,
                Default = new bool[0],
            },
            new ProgramArgumentInfo
            {
                Name = "validate",
                Description = SlnGenStrings.validateUsage,
                Default = false,
            },
            new ProgramArgumentInfo
            {
                Name = "devenv",
                Type = typeof(string),
                Description = SlnGenStrings.devenvUsage,
                Default = null,
            },
            new ProgramArgumentInfo
            {
                Name = "error",
                Type = typeof(string[]),
                Description = SlnGenStrings.errorUsage,
                Default = new string[0],
            },
            new ProgramArgumentInfo
            {
                Name = "version",
                ShortForms = {"ver"},
                Type = typeof(int),
                Description = SlnGenStrings.versionUsage,
            },
            new ProgramArgumentInfo
            {
                Name = "purge",
                Type = typeof(bool),
                Description = SlnGenStrings.purgeUsage,
                Default = false,
            },
            new ProgramArgumentInfo
            {
                Name = "property",
                Type = typeof(string[]),
                ShortForms = {"p"},
                Description = SlnGenStrings.propertyUsage,
                Default = new string[0]
            },
            new ProgramArgumentInfo
            {
                Name = "relative",
                Type = typeof(bool),
                ShortForms = {"rel"},
                Description = SlnGenStrings.relativeUsage,
                Default = true
            },
            new ProgramArgumentInfo
            {
                Name = "help",
                ShortForms = {"?", "h"},
                Description = ProgramArgumentsStrings.HelpArgumentDescription,
                Default = false
            }
        };

        private readonly Dictionary<string, string> _globalProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public SlnGenProgramArguments(string[] args)
            : base(args, Infos)
        {
            ExitCode = ProgramExitCode.Success;

            int verbose = GetValue<bool[]>("verbose").Sum(delta => delta ? 1 : -1) - GetValue<bool[]>("quiet").Sum(delta => delta ? 1 : -1);

            Log.SetVerbosity(verbose);

            char[] sep = {'='};

            foreach (string prop in GetValue<string[]>("property"))
            {
                string[] kvp = prop.Split(sep, 2);

                if (kvp.Length != 2)
                {
                    throw new ArgumentException($"Invalid property '{prop}'; properties must be specified in the form /p:name=value");
                }

                _globalProperties.Add(kvp[0], kvp[1]);
            }

            if (UsageException != null)
            {
                ExitCode = ProgramExitCode.BadOrMissingArgumentError;
            }

            InitialProjects.AddRange(GetValue<string[]>("files"));

            CurrentDirectory = Directory.GetCurrentDirectory();

            foreach (string error in GetValue<string[]>("error"))
            {
                SlnError.ChangeEnabled(error);
            }

            TemporaryDirectory = Path.Combine(Path.GetTempPath(), "SlnGen");
        }

        public string CurrentDirectory { get; }

        public string DevenvArgs
        {
            get
            {
                // TODO: Make this a method
                StringBuilder procArgs = new StringBuilder();
                if (InHive)
                {
                    procArgs.Append("/rootSuffix Exp /RANU ");
                }

                if (Run)
                {
                    procArgs.Append("/run ");
                    procArgs.Append(ProjectConfiguration);
                    procArgs.Append(" ");
                }

                procArgs.Append('"');
                procArgs.Append(SlnFile);
                procArgs.Append('"');
                return procArgs.ToString();
            }
        }

        public string DevenvExe
        {
            get
            {
                // TODO: Make this a method
                // If user specify the devenv.exe, return it regardless
                if (!String.IsNullOrEmpty(ExplicitDevenvExe))
                {
                    return ExplicitDevenvExe;
                }

                // Try to look at the place devenv.exe suppose to be first
                string devenvExe = "devenv.exe";

                if (!String.IsNullOrEmpty(VisualStudio.ToolsPath))
                {
                    string vsPath = Path.Combine(Directory.GetParent(VisualStudio.ToolsPath).FullName, "IDE");
                    devenvExe = Path.Combine(vsPath, "devenv.exe");
                }

                // If can't find it, look at magical places
                if (!File.Exists(devenvExe))
                {
                    List<string> values = Environment.GetEnvironmentVariable("VSDEVENV").ExtractQuoted();
                    if (values.Count != 0)
                    {
                        return Path.GetFullPath(values[0]);
                    }

                    values = ((string) Registry.GetValue("HKEY_CLASSES_ROOT\\VisualStudio.Launcher.sln\\Shell\\Open\\Command", null, null)).ExtractQuoted();

                    if (values.Count != 0)
                    {
                        return Path.GetFullPath(values[0]);
                    }

                    // TODO: Use vswhere.exe

                    // Giving up.
                    Log.Info("Can't find a specified path to devenv.exe or a VS launcher. Tried: command line, VSDEVENV environment variable, and VisualStudio.Launcher.sln reg key. Will try looking in path for devenv.exe.");
                }
                return devenvExe;
            }
        }

        public ProgramExitCode ExitCode { get; }

        public string ExplicitDevenvExe
        {
            get
            {
                List<string> values = ExtensionMethods.ExtractQuoted(GetValue<string>("devenv"));

                return values.Count != 0 ? Path.GetFullPath(values[0]) : null;
            }
        }

        public IDictionary<string, string> GlobalProperties => _globalProperties;

        public int IncludeReverse => GetValue<int>("includeReverse");

        public int IncludeReverseLevel => GetValue<int>("includeReverseLevel");

        public bool InHive => GetValue<bool>("hive");

        public List<string> InitialProjects { get; } = new List<string>();

        public bool LaunchVisualStudio => !(GetValue<bool>("noLaunch") || ValidateOnly);

        public bool Nest => GetValue<bool>("nest");

        public string ProjectConfiguration => GetValue<string>("run");

        public bool Purge => GetValue<bool>("purge");

        public bool Recurse => GetValue<bool>("recurse");

        public bool RelativePaths => GetValue<bool>("relative");

        public bool Run => GetValue<string>("run") != null;

        public string SlnDir { get; private set; }

        public string SlnFile { get; private set; }

        public string TemporaryDirectory { get; }

        public bool UseCurrentDirectory => GetValue<bool>("useCurrent");

        public bool UseWorkspaceDirectory => GetValue<bool>("useWorkspace");

        public bool ValidateOnly => GetValue<bool>("validate");

        public IVisualStudioVersion VisualStudio
        {
            get
            {
                int version = GetValue<int>("version");
                switch (version)
                {
                    case 10:
                        return new VisualStudio2010();

                    case 11:
                        return new VisualStudio2012();

                    case 12:
                        return new VisualStudio2013();

                    case 14:
                        return new VisualStudio2015();

                    case 15:
                        return new VisualStudio2017();

                    default:
                        Log.Info($"Visual Studio version {version} is not tested with SlnGen, use it at your own risk.");
                        return new VisualStudioGeneric(new Version(version, 0));
                }
            }
        }

        public string[] WorkspaceFiles => GetValue<string>("workspaceFiles").Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);

        public string WorkspaceSubDirectory => GetValue<string>("WorkspaceSubDir");

        public static int CleanupTempSolutionDirectories(string directory)
        {
            int count = 0;
            foreach (string dir in Directory.GetDirectories(directory, TempSolutionDirPrefix + "*"))
            {
                try
                {
                    Directory.Delete(dir, true);
                    count++;
                }
                catch (IOException e)
                {
                    Log.Error($"Can't delete directory {Path.GetFileName(dir)}: {e.Message}.");
                }
            }
            return count;
        }

        /// <summary>
        /// Creates the .sln target file name
        /// </summary>
        public static string CreateSlnFilename(string targetDirectory, string slnFilename)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(slnFilename);

            string projName = Path.GetFileNameWithoutExtension(slnFilename);

            if (projName == "dirs")
            {
                projName = $"{directoryInfo.Parent}_{projName}";
            }
            else
            {
                string subDir = Path.GetDirectoryName(slnFilename);
                if (!String.IsNullOrEmpty(subDir))
                {
                    projName = Path.Combine(subDir, projName);
                }
            }

            if (!String.IsNullOrEmpty(targetDirectory))
            {
                projName = Path.Combine(targetDirectory, projName);
            }

            return projName + ".sln";
        }

        public string GetSlnRoot()
        {
            string workspaceRoot = GetWorkspaceRoot();
            if (workspaceRoot == null)
            {
                return null;
            }
            if (String.IsNullOrEmpty(WorkspaceSubDirectory))
            {
                return workspaceRoot;
            }
            return Path.Combine(workspaceRoot, WorkspaceSubDirectory);
        }

        public string GetWorkspaceRoot()
        {
            if (!UseWorkspaceDirectory)
            {
                return null;
            }

            string[] files = WorkspaceFiles;
            if (files == null || files.Length == 0)
            {
                return null;
            }

            string dir = Directory.GetCurrentDirectory();
            while (dir != null)
            {
                if (files.Any(file => File.Exists(Path.Combine(dir, file))))
                {
                    return dir;
                }
                dir = Path.GetDirectoryName(dir);
            }

            return null;
        }

        /// <summary>
        /// Attempts to apply logical defaults for unspecified behavior.
        /// </summary>
        public void GuessDefaults()
        {
            if (InitialProjects.Count == 0)
            {
                if (Recurse)
                {
                    // We are doing recursion on an unspecified directory (this directory).
                    InitialProjects.Add(CurrentDirectory);
                }
                else
                {
                    // We need to find an unspecified file.
                    InitialProjects.AddRange(Directory.EnumerateFiles(CurrentDirectory, "*.*proj"));
                }
            }

            // determine where we should put our output.
            SlnFile = ComputeSlnFileFullName();

            Log.Verbose($"Creating {SlnFile}.");

            SlnDir = Directory.GetParent(SlnFile).FullName;

            Directory.CreateDirectory(SlnDir);
        }

        private string ComputeSlnFileFullName()
        {
            string slnName = null;

            // precedence order for determining the directory:
            // outputFile, useCurrent, useWorkspace, useTemp
            string outputFile = GetValue<string>("outputFile");
            if (outputFile != null)
            {
                slnName = CreateSlnFilename(String.Empty, outputFile);
                // full path specified ==> we're done
                if (slnName.IndexOf(':') != -1)
                {
                    return slnName;
                }
            }

            DirectoryInfo currDir = new DirectoryInfo(Directory.GetCurrentDirectory());

            if (String.IsNullOrEmpty(slnName))
            {
                slnName = InitialProjects.Count > 0 ? Path.GetFileName(InitialProjects[0]) : currDir.Name;
            }

            if (UseCurrentDirectory)
            {
                return CreateSlnFilename(currDir.FullName, slnName);
            }

            string slnRoot = GetSlnRoot();
            if (slnRoot != null)
            {
                return CreateSlnFilename(slnRoot, slnName);
            }

            return CreateSlnFilename(Path.Combine(TemporaryDirectory, $"{TempSolutionDirPrefix}{Guid.NewGuid()}"), slnName);
        }
    }
}