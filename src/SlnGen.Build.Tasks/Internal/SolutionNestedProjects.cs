using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SlnGen.Build.Tasks.Internal
{
    /// <summary>
    /// Represents the hierarchy of projects in a Visual Studio solution.
    /// </summary>
    /// <remarks>This assumes all projects are on the same drive.</remarks>
    internal sealed class SolutionHierarchy
    {
        private readonly List<SolutionFolder> _folders = new List<SolutionFolder>();
        private readonly Dictionary<string, string> _hierarchy = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _itemId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public SolutionHierarchy(IReadOnlyList<SolutionProject> projects)
        {
            string commonPrefix = new string(
                projects.First(e => !e.IsMainProject).FullPath.Substring(0, projects.Min(s => s.FullPath.Length))
                    .TakeWhile((c, i) => projects.All(s => s.FullPath[i] == c)).ToArray());

            foreach (SolutionProject project in projects)
            {
                if (project.IsMainProject)
                {
                    continue;
                }

                BuildHierarchyBottomUp(project, commonPrefix.TrimEnd(Path.DirectorySeparatorChar));
            }
        }

        public IReadOnlyCollection<SolutionFolder> Folders => _folders;

        public IReadOnlyDictionary<string, string> Hierarchy => _hierarchy;

        private void BuildHierarchyBottomUp(SolutionProject project, string root)
        {
            string parent = Directory.GetParent(project.FullPath).FullName;
            string currentGuid = project.ProjectGuid;

            while (true)
            {
                bool visited = _itemId.TryGetValue(parent, out string parentGuid);
                if (!visited)
                {
                    parentGuid = $"{{{Guid.NewGuid().ToString().ToUpperInvariant()}}}";
                    _itemId.Add(parent, parentGuid);
                    _folders.Add(new SolutionFolder(parent, parentGuid));
                }

                _hierarchy[currentGuid] = parentGuid;

                if (visited || parent.Equals(root, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                currentGuid = parentGuid;
                parent = Directory.GetParent(parent).FullName;
            }
        }
    }
}