using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;

namespace SlnGen
{
    internal class ProjectInfo
    {
        private const string TestProjectTypeGuid = "{3AC096D0-A1C2-E12C-1390-A8335801FDAB}";

        private static readonly Guid BadGuid = new Guid("{9501A45E-ACCA-4B96-84E0-8A12E20D88CA}");

        private static readonly Dictionary<string, Guid?> ProjectExtensionGuids = InitializeProjectExtensionGuids();

        private readonly Guid? _projectTypeGuid;

        private Guid _guid;

        public ProjectInfo(string pathname, int level, IDictionary<string, string> globalProperties, ProjectCollection projectCollection)
        {
            ProjectPath = ProjectPath.FromFile(pathname);
            Level = level;

            if (!File.Exists(ProjectPath.FullName))
            {
                SlnError.ReportError(SlnError.ErrorId.MissingFile, ProjectPath.ToString());
                return;
            }

            try
            {
                Project = new Project(ProjectPath.FullName, globalProperties, null, projectCollection);
            }
            catch (InvalidProjectFileException e)
            {
                SlnError.ReportError(SlnError.ErrorId.LoadFailed, ProjectPath.ToString(), e.BaseMessage);
                Project = null;
                return;
            }

            Valid = true;

            // If we can't figure out what type of project this is, we may report an error
            if (!ProjectExtensionGuids.TryGetValue(ProjectPath.Extension, out _projectTypeGuid))
            {
                Log.Info("Unknown project extension {0} encountered.", ProjectPath.Extension);
            }

            // Check to see if this project has any Compile build items
            if (Project.GetItems("Compile").Count > 0)
            {
                Compilable = true;
            }

            // If a project guid is already defined, use it or else create a new one
            try
            {
                _guid = new Guid(Project.GetPropertyValue("ProjectGuid"));
            }
            catch (FormatException)
            {
                _guid = Guid.NewGuid();

                if (Compilable)
                {
                    Valid = false;
                    string projectGuid = Project.GetPropertyValue("ProjectGuid");
                    string[] message;
                    if (String.IsNullOrWhiteSpace(projectGuid))
                    {
                        message = new[]
                        {
                            "Missing ProjectGuid in project file. Please add one with a newly generated guid such as the following:",
                            $"<ProjectGuid>{FormattedGuid}</ProjectGuid>"
                        };
                    }
                    else
                    {
                        message = new[]
                        {
                            $"Malformed ProjectGuid {projectGuid}.",
                            "Please edit the project file and insert a valid guid. Here is one you can use:",
                            FormattedGuid
                        };
                    }

                    SlnError.ReportError(SlnError.ErrorId.MissingOrBadGuid, ProjectPath.ToString(), message);
                }
            }

            if (Compilable)
            {
                if (_guid == BadGuid)
                {
                    SlnError.ReportError(SlnError.ErrorId.MissingOrBadGuid, ProjectPath.ToString(), "Bad GUID", BadGuid.ToString("B"));
                }

                if (String.IsNullOrEmpty(Version))
                {
                    SlnError.ReportError(SlnError.ErrorId.MissingVersion, ProjectPath.ToString());
                }

                if (String.IsNullOrEmpty(AssemblyName))
                {
                    SlnError.ReportError(SlnError.ErrorId.MissingAssemblyName, ProjectPath.ToString());
                }
            }

            // Does the project name match the assembly name?
            if (!String.IsNullOrEmpty(AssemblyName) && !AssemblyName.Equals(ProjectPath.ProjectName, StringComparison.OrdinalIgnoreCase))
            {
                SlnError.ReportError(SlnError.ErrorId.MismatchedAssemblyName, ProjectPath.ToString(), $"Assembly name is: {AssemblyName}");
            }
        }

        public string AssemblyName => Project.GetPropertyValue("AssemblyName");

        public bool Compilable { get; }

        public string FormattedGuid => _guid.ToString("B").ToUpper();

        public bool IsKnownProjectType => _projectTypeGuid.HasValue;

        public bool IsTestProject => Project.GetPropertyValue("ProjectTypeGuids").Contains(TestProjectTypeGuid);

        public int Level { get; set; }

        public bool Processed { get; private set; }

        public Project Project { get; }

        public bool Valid { get; }

        public string Version => Project.GetPropertyValue("ProductVersion");
        public ProjectPath ProjectPath { get; }

        /// <summary>
        /// Finds all references to other projects from this one.
        /// </summary>
        public void FindAllReferences(ProjectClosure closure)
        {
            Processed = true;
            Project.ReevaluateIfNecessary();
            FindAllReferences(closure, "ProjectReference", true);
            FindAllReferences(closure, "ProjectFile", false);
        }

        internal string GetProjectSection(bool useRelativePath, string slnPath)
        {
            return $"Project(\"{_projectTypeGuid?.ToString("B").ToUpper()}\") = \"{ProjectPath.ProjectName}\", \"{(useRelativePath ? MakeRelative(slnPath, ProjectPath.FullName) : ProjectPath.FullName)}\", \"{FormattedGuid}\"\r\nEndProject\r\n";
        }

        private static Uri CreateUriFromPath(string path)
        {
            if (!Uri.TryCreate(path, UriKind.Absolute, out Uri result))
            {
                result = new Uri(path, UriKind.Relative);
            }
            return result;
        }

        private static Dictionary<string, Guid?> InitializeProjectExtensionGuids()
        {
            Dictionary<string, Guid?> result = new Dictionary<string, Guid?>(StringComparer.OrdinalIgnoreCase);

            if (!(ConfigurationManager.GetSection("projectExtensions") is NameValueCollection projectExtensions))
            {
                return result;
            }

            foreach (string key in projectExtensions.Keys)
            {
                Guid? val = null;
                if (!String.IsNullOrEmpty(projectExtensions[key]))
                {
                    val = new Guid(projectExtensions[key]);
                }

                result.Add($".{key}", val);
            }
            return result;
        }

        // Shamelessly stolen from Microsoft.Build.Shared.FileUtilities.MakeRelative.
        private static string MakeRelative(string basePath, string path)
        {
            if (String.IsNullOrEmpty(basePath))
            {
                return path;
            }
            if (basePath[basePath.Length - 1] != Path.DirectorySeparatorChar)
            {
                basePath += Path.DirectorySeparatorChar;
            }
            Uri uriBasePath = new Uri(basePath, UriKind.Absolute);
            Uri uriPath = CreateUriFromPath(path);
            if (!uriPath.IsAbsoluteUri)
            {
                uriPath = new Uri(uriBasePath, uriPath);
            }
            Uri uriRet = uriBasePath.MakeRelativeUri(uriPath);
            string ret = Uri.UnescapeDataString(uriRet.IsAbsoluteUri ? uriRet.LocalPath : uriRet.ToString());
            return ret.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        private void FindAllReferences(ProjectClosure closure, string itemName, bool isReference)
        {
            foreach (ProjectItem projectReference in Project.GetItems(itemName))
            {
                string referencePath = projectReference.EvaluatedInclude;
                if (String.IsNullOrEmpty(referencePath))
                {
                    continue;
                }

                referencePath = Path.Combine(ProjectPath.DirectoryName, referencePath);
                referencePath = Path.GetFullPath(referencePath);

                if (!File.Exists(referencePath))
                {
                    continue;
                }
                closure.AddEntriesToParseFiles(referencePath, false, projectReference.GetMetadataValue("AdditionalProperties"), isReference ? Level + 1 : Level);
                if (isReference)
                {
                    closure.AddProjectReference(ProjectPath.FullName, referencePath);
                }
            }
        }
    }
}