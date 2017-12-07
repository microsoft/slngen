using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SlnGen
{
    internal sealed class ProjectClosure
    {
        private readonly SlnGenProgramArguments _arguments;

        private readonly Dictionary<string, ProjectInfo> _guidToProject = new Dictionary<string, ProjectInfo>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, ProjectInfo> _nameToProject = new Dictionary<string, ProjectInfo>(StringComparer.OrdinalIgnoreCase);

        private readonly ProjectCollection _projectCollection = new ProjectCollection();

        private readonly Dictionary<string, List<string>> _projectReferences = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        private readonly bool _trackReferences;

        public ProjectClosure(SlnGenProgramArguments arguments, bool trackReferences)
        {
            // TODO: Pass global properties instead of SlnGenProgramArguments
            _arguments = arguments;
            _trackReferences = trackReferences;
        }

        public enum FileType
        {
            Bad,
            File,
            Directory
        }

        public List<ProjectInfo> ActualProjects { get; } = new List<ProjectInfo>();

        public NestingDir Nesting { get; } = new NestingDir();

        public Dictionary<ProjectPath, ProjectInfo> ProjectInfo { get; } = new Dictionary<ProjectPath, ProjectInfo>();

        public int ProjectsLoadedCount { get; private set; }

        public Stack<ProjectToLoad> ProjectsToLoad { get; } = new Stack<ProjectToLoad>();

        /// <summary>
        /// Determines if a file/directory exists and appears to be accessible.
        /// </summary>
        public static FileType GetFileType(string projectFile)
        {
            try
            {
                FileAttributes attributes = File.GetAttributes(projectFile);

                if ((attributes & (FileAttributes.System | FileAttributes.Hidden | FileAttributes.Offline)) != 0)
                {
                    return FileType.Bad;
                }

                if ((attributes & FileAttributes.Directory) != 0)
                {
                    return FileType.Directory;
                }

                return FileType.File;
            }
            catch (IOException)
            {
                Log.Error("Bad file or directory: {0}.", projectFile);
                return FileType.Bad;
            }
        }

        /// <summary>
        /// Processes directories and sub-directories and loads all the project files within them.
        /// </summary>
        public ProgramExitCode AddEntriesToParseFiles(IEnumerable<string> pathnames, bool recurse)
        {
            ProgramExitCode result = ProgramExitCode.Success;

            foreach (string pathname in pathnames)
            {
                ProgramExitCode temp = AddEntriesToParseFiles(pathname, recurse, null, 1);
                if (temp != ProgramExitCode.Success)
                {
                    result = temp;
                }
            }

            return result;
        }

        /// <summary>
        /// Processes a directory and the sub-directories and loads all project files within them.
        /// </summary>
        public ProgramExitCode AddEntriesToParseFiles(string pathname, bool recurse, string additionalProperties, int level)
        {
            switch (GetFileType(pathname))
            {
                case FileType.Bad:
                    SlnError.ReportError(
                        SlnError.ErrorId.MissingFile, pathname, "Missing, system, or unreadable file encountered.");
                    return ProgramExitCode.BadProjectNameError;

                case FileType.File:
                    ProjectsToLoad.Push(new ProjectToLoad(pathname, additionalProperties, level));
                    return ProgramExitCode.Success;

                case FileType.Directory:
                    foreach (string file in Directory.GetFiles(pathname, "*.*proj", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                    {
                        ProjectsToLoad.Push(new ProjectToLoad(file, additionalProperties, level));
                    }
                    return ProgramExitCode.Success;

                default:
                    Log.Error("Bad internal (SlnGen) state for {0}.", pathname);
                    return ProgramExitCode.BadProjectNameError;
            }
        }

        public void AddProjectReference(string source, string destination)
        {
            if (!_trackReferences)
            {
                return;
            }

            if (!_projectReferences.TryGetValue(destination, out List<string> sources))
            {
                sources = new List<string>();
                _projectReferences.Add(destination, sources);
            }

            sources.Add(source);
            Log.ReallyVerbose("Adding reference {0}=>{1}.", source, destination);
        }

        /// <summary>
        /// Creates a VS Solution file listing all the projects that have been analyzed.
        /// </summary>
        public void CreateTempSlnFile(string slnFile, bool nest, bool useRelativeToSolutionPath, IVisualStudioVersion visualStudio)
        {
            // Build the Project section string
            StringBuilder projectSection = new StringBuilder();
            StringBuilder dirSection = new StringBuilder();
            StringBuilder hierarchy = new StringBuilder();
            NestingDir root = Nesting.DisplayRoot;
            if (nest)
            {
                hierarchy.AppendLine("Global");
                hierarchy.AppendLine("\tGlobalSection(NestedProjects) = preSolution");
            }
            string slnPath = Path.GetDirectoryName(slnFile);

            foreach (ProjectInfo project in ActualProjects.Where(item => item.IsKnownProjectType))
            {
                projectSection.Append(project.GetProjectSection(useRelativeToSolutionPath, slnPath));

                if (nest)
                {
                    NestingDir dir = Nesting.GetDir(project.ProjectPath);
                    if (dir.HasOnlySelfNamedProject)
                    {
                        dir = dir.Parent;
                    }

                    if (dir != root)
                    {
                        hierarchy.AppendFormat(
                            "\t\t{0} = {1}\r\n",
                            project.FormattedGuid,
                            dir.FormattedGuid);
                    }
                }
                ProjectsLoadedCount++;
            }

            dirSection.Append(root.GetFormattedDirProject(true));

            if (nest)
            {
                foreach (NestingDir node in root.AllChildren.Where(item => !item.HasOnlySelfNamedProject))
                {
                    dirSection.Append(node.GetFormattedDirProject());

                    if (node.Parent != root)
                    {
                        hierarchy.AppendFormat(
                            "\t\t{0} = {1}\r\n",
                            node.FormattedGuid,
                            node.Parent.FormattedGuid);
                    }
                }

                hierarchy.AppendLine("\tEndGlobalSection");
                hierarchy.AppendLine("EndGlobal");
            }

            using (StreamWriter writer = new StreamWriter(slnFile))
            {
                writer.WriteLine("Microsoft Visual Studio Solution File, Format Version {0}", visualStudio.FileFormatVersion);
                writer.WriteLine("# Visual Studio {0}", visualStudio.Version);
                writer.Write(projectSection.ToString());
                writer.Write(dirSection.ToString());
                writer.Write(hierarchy.ToString());

                writer.WriteLine();
            }

            foreach (KeyValuePair<string, string> companionFile in root.GetAllCompanionFiles())
            {
                string targetName = String.Format(companionFile.Key, Path.GetFileNameWithoutExtension(slnFile));
                string targetPath = Path.Combine(Directory.GetParent(slnFile).FullName, targetName);
                File.Copy(companionFile.Value, targetPath, true);
                File.SetAttributes(targetPath, File.GetAttributes(targetPath) & ~FileAttributes.ReadOnly);
            }
        }

        public IEnumerable<string> GetChildProjects(IEnumerable<string> projects)
        {
            foreach (string project in projects)
            {
                Log.ReallyVerbose("looking for reverse dependencies of {0}.", project);

                if (!_projectReferences.TryGetValue(project, out List<string> sources))
                {
                    continue;
                }

                foreach (string source in sources)
                {
                    yield return source;
                }
            }
        }

        /// <summary>
        /// Finds (or creates) project info for all projects
        /// </summary>
        public ProjectInfo GetOrCreateProjectInfo(ProjectToLoad projectToLoad)
        {
            string path = projectToLoad.FullPath;
            ProjectPath vsPath = ProjectPath.FromFile(path);

            if (!ProjectInfo.TryGetValue(vsPath, out ProjectInfo project))
            {
                IDictionary<string, string> mergedProperties = MergeProperties(_arguments.GlobalProperties, projectToLoad.AdditionalProperties);
                project = new ProjectInfo(vsPath.FullName, projectToLoad.Level, mergedProperties, _projectCollection);
                ProjectInfo[vsPath] = project;

                if (_guidToProject.ContainsKey(project.FormattedGuid))
                {
                    SlnError.ReportError(
                        SlnError.ErrorId.DuplicateGuid,
                        vsPath.ToString(),
                        _guidToProject[project.FormattedGuid].ProjectPath.ToString(), project.FormattedGuid);
                }
                _guidToProject[project.FormattedGuid] = project;

                // CreateTempSlnFile ignores all projects where IsKnownProjectType is false
                if (project.IsKnownProjectType)
                {
                    string name = project.ProjectPath.ProjectName;
                    if (_nameToProject.ContainsKey(name))
                    {
                        SlnError.ReportError(
                            SlnError.ErrorId.DuplicateProjectName,
                            vsPath.ToString(),
                            _nameToProject[name].ProjectPath.ToString(),
                            "(VS cannot handle duplicate project names)");
                    }
                    _nameToProject[name] = project;
                }
            }
            else
            {
                project.Level = Math.Min(project.Level, projectToLoad.Level);
            }

            return project;
        }

        /// <summary>
        /// Loads the project files into memory and processes them
        /// </summary>
        public bool ProcessProjectFiles()
        {
            bool allProjectsValid = true;
            while (ProjectsToLoad.Count > 0)
            {
                // Pick up a project to process, from the stack
                ProjectInfo projectLoad = GetOrCreateProjectInfo(ProjectsToLoad.Pop());
                if (!projectLoad.Valid)
                {
                    allProjectsValid = false;
                    continue;
                }
                if (projectLoad.Processed)
                {
                    continue;
                }

                Log.Verbose("Processing Project {0}", projectLoad.ProjectPath);

                try
                {
                    projectLoad.FindAllReferences(this);
                    ActualProjects.Add(projectLoad);
                    Nesting.AddProjectFile(projectLoad);
                }
                catch (Exception e) when (!e.IsFatal())
                {
                    // We want to continue loading the other projects even if one fails
                    SlnError.ReportError(SlnError.ErrorId.LoadFailed, projectLoad.ProjectPath.FullName, e.Message);
                }
            }

            foreach (ProjectInfo projectInfo in ActualProjects)
            {
                Log.ReallyVerbose($"Project '{projectInfo.ProjectPath}' is of level {projectInfo.Level}.");
            }

            return allProjectsValid;
        }

        /// <summary>
        /// Closure overlap checking
        /// </summary>
        public bool ValidateNoOverlap(string topLevel, ProjectClosure other, string otherTopLevel)
        {
            bool result = true;

            foreach (ProjectPath path in ProjectInfo.Keys.Where(path => other.ProjectInfo.ContainsKey(path)))
            {
                SlnError.ReportError(
                    SlnError.ErrorId.ProjectsOverlapped,
                    path.ToString(),
                    "Referenced in: ", topLevel, otherTopLevel);
                result = false;
            }

            return result;
        }

        private static IDictionary<string, string> MergeProperties(IDictionary<string, string> globalProperties, IDictionary<string, string> additionalProperties)
        {
            IDictionary<string, string> result = globalProperties;
            if (additionalProperties != null && additionalProperties.Count > 0)
            {
                result = new Dictionary<string, string>(additionalProperties, StringComparer.OrdinalIgnoreCase);
                foreach (KeyValuePair<string, string> globalProperty in globalProperties)
                {
                    result[globalProperty.Key] = globalProperty.Value;
                }
            }
            return result;
        }
    }
}