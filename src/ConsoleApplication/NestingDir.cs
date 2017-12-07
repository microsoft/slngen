using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SlnGen
{
    internal sealed class NestingDir
    {
        public NestingDir()
            : this(null, SlnGen.ProjectPath.EmptyPath)
        {
        }

        private NestingDir(NestingDir theParent, ProjectPath thePath)
        {
            ProjectPath = thePath;
            RealGuid = Guid.NewGuid();

            FormattedGuid = RealGuid.ToString("B");

            Parent = theParent;
            Children = new SortedList<string, NestingDir>(StringComparer.OrdinalIgnoreCase);

            RealRoot = Parent == null ? this : Parent.RealRoot;

            Projects = new SortedList<string, string>(StringComparer.OrdinalIgnoreCase);
            SolutionItems = new SortedList<string, string>(StringComparer.OrdinalIgnoreCase);
            CompanionFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public static string TypeGuid => "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

        public List<NestingDir> AllChildren
        {
            get
            {
                List<NestingDir> result = new List<NestingDir>();
                foreach (NestingDir child in Children.Values)
                {
                    result.Add(child);
                    result.AddRange(child.AllChildren);
                }

                return result;
            }
        }

        public SortedList<string, NestingDir> Children { get; }
        public Dictionary<string, string> CompanionFiles { get; }

        public NestingDir DisplayRoot
        {
            get
            {
                NestingDir result = RealRoot;

                while (result.Children.Count == 1 && result.Projects.Count == 0 && result.SolutionItems.Count == 0)
                {
                    result = result.Children.Values[0];
                }

                return result;
            }
        }

        public string FormattedGuid { get; }
        public bool HasOnlySelfNamedProject => Children.Count == 0 && Projects.Count == 1 && Projects.ContainsKey(ProjectPath.VsName);
        public NestingDir Parent { get; }
        public SortedList<string, string> Projects { get; }
        public Guid RealGuid { get; }
        public NestingDir RealRoot { get; }
        public SortedList<string, string> SolutionItems { get; }
        public ProjectPath ProjectPath { get; }

        public void AddProjectFile(ProjectInfo project)
        {
            NestingDir node = GetDir(project.ProjectPath);
            node.Projects[project.ProjectPath.ProjectName] = project.ProjectPath.ProjectName;

            if (project.IsTestProject)
            {
                DirectoryInfo projectDir = new DirectoryInfo(project.ProjectPath.DirectoryName);
                foreach (FileInfo settingsFile in projectDir.EnumerateFiles("*.testsettings"))
                {
                    node.SolutionItems[settingsFile.FullName] = settingsFile.FullName;
                }
            }

            foreach (ProjectItem solutionItem in project.Project.GetItems("SlnGenSolutionItem"))
            {
                node.SolutionItems[solutionItem.EvaluatedInclude] = solutionItem.EvaluatedInclude;
            }

            foreach (ProjectItem companionFile in project.Project.GetItems("SlnGenCompanionFile"))
            {
                string targetNamePattern = companionFile.GetMetadataValue("TargetNamePattern");
                if (String.IsNullOrEmpty(targetNamePattern))
                {
                    targetNamePattern = Path.GetFileName(companionFile.EvaluatedInclude);
                }
                node.CompanionFiles[targetNamePattern] = companionFile.EvaluatedInclude;
            }
        }

        public IEnumerable<KeyValuePair<string, string>> GetAllCompanionFiles()
        {
            return GetAllItems((nd) => nd.CompanionFiles, true);
        }

        public NestingDir GetDir(ProjectPath path)
        {
            NestingDir current = RealRoot;

            for (int i = path.PathComponents.Count - 1; i >= 0; i--)
            {
                ProjectPath currentPath = path.GetAncestorDirectory(i);
                string name = currentPath.VsName;

                if (!current.Children.ContainsKey(name))
                {
                    current.Children.Add(name, new NestingDir(current, currentPath));
                }

                current = current.Children[name];
            }

            return current;
        }

        public string GetFormattedDirProject(bool root = false)
        {
            // VS cares about the exact name of the "Solution Items" folder.
            // If it has an underscore in front of it, VS will not auto-recognize .testsettings files and will not auto-create a vsmdi file.  This means that nesting breaks this functionality.
            string projectFormat = root ? "Project(\"{0}\") = \"{1}\", \"{1}\", \"{2}\"\r\n{3}EndProject\r\n" : "Project(\"{0}\") = \"_{1}\", \"_{1}\", \"{2}\"\r\n{3}EndProject\r\n";

            string solutionItems = root ? GetFormattedSolutionItems() : String.Empty;

            // For root case, if we have no items return blank.
            if (root && String.IsNullOrEmpty(solutionItems))
            {
                return String.Empty;
            }

            string itemName = root ? "Solution Items" : ProjectPath.VsName;
            return String.Format(projectFormat, TypeGuid, itemName, FormattedGuid, solutionItems);
        }

        private IEnumerable<T> GetAllItems<T>(Func<NestingDir, IEnumerable<T>> getItems, bool recursive)
        {
            IEnumerable<T> result = getItems(this);
            if (recursive)
            {
                result = result.Union(AllChildren.SelectMany(getItems));
            }
            return result;
        }

        private string GetFormattedSolutionItems()
        {
            List<string> allSolutionItems = GetAllItems((nd) => nd.SolutionItems.Values, true).ToList();

            if (allSolutionItems.Count == 0)
            {
                return String.Empty;
            }

            StringBuilder solutionItemsFormat = new StringBuilder();
            solutionItemsFormat.AppendLine("\tProjectSection(SolutionItems) = preProject");

            // Ignore duplicates
            foreach (string solutionItem in allSolutionItems.Distinct())
            {
                solutionItemsFormat.AppendFormat("\t\t{0} = {0}\r\n", solutionItem);
            }

            solutionItemsFormat.AppendLine("\tEndProjectSection");
            return solutionItemsFormat.ToString();
        }
    }
}