// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

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
    internal sealed class SlnHierarchy
    {
        private readonly List<SlnFolder> _folders = new List<SlnFolder>();
        private readonly Dictionary<Guid, Guid> _hierarchy = new Dictionary<Guid, Guid>();
        private readonly Dictionary<SlnItem, Guid> _itemHierarchy = new Dictionary<SlnItem, Guid>();
        private readonly Dictionary<string, Guid> _itemId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        private SlnHierarchy()
        {
        }

        public IReadOnlyCollection<SlnFolder> Folders => _folders;

        public IReadOnlyDictionary<Guid, Guid> Hierarchy => _hierarchy;

        public IReadOnlyDictionary<SlnItem, Guid> ItemHierarchy => _itemHierarchy;

        public static SlnHierarchy FromProjectsAndItems(IReadOnlyList<SlnProject> projects, IReadOnlyList<SlnItem> items)
        {
            SlnHierarchy hierarchy = new SlnHierarchy();

            List<string> paths = projects.Where(i => !i.IsMainProject).Select(i => Directory.GetParent(i.FullPath).FullName).ToList();
            paths.AddRange(items.Select(i => i.FullPath));

            if (paths.Count > 1)
            {
                string commonPrefix = GetCommonDirectoryPath(paths);

                foreach (SlnProject project in projects)
                {
                    if (project.IsMainProject)
                    {
                        continue;
                    }

                    hierarchy.BuildHierarchyBottomUp(project, commonPrefix.TrimEnd(Path.DirectorySeparatorChar));
                }

                foreach (SlnItem item in items)
                {
                    hierarchy.BuildHierarchyBottomUp(item, commonPrefix.TrimEnd(Path.DirectorySeparatorChar));
                }
            }

            return hierarchy;
        }

        public static string GetCommonDirectoryPath(IReadOnlyList<string> paths)
        {
            // TODO: Unit tests, optimize
            string commonPath = String.Empty;

            List<string> separatedPath = paths
                .First(str => str.Length == paths.Max(st2 => st2.Length))
                .Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            foreach (string pathSegment in separatedPath.AsEnumerable())
            {
                if (commonPath.Length == 0 && paths.All(str => str.StartsWith(pathSegment)))
                {
                    commonPath = pathSegment;
                }
                else if (paths.All(str => str.StartsWith(commonPath + Path.DirectorySeparatorChar + pathSegment)))
                {
                    commonPath += Path.DirectorySeparatorChar + pathSegment;
                }
                else
                {
                    break;
                }
            }

            return commonPath;
        }

        private void BuildHierarchyBottomUp(SlnProject project, string root)
        {
            // TODO: Collapse folders with single sub folder.  So if foo had just a subfolder bar, collapse it to foo\bar in Visual Studio
            string parent = Directory.GetParent(project.FullPath).FullName;
            Guid currentGuid = project.ProjectGuid;

            while (true)
            {
                bool visited = _itemId.TryGetValue(parent, out Guid parentGuid);
                if (!visited)
                {
                    parentGuid = Guid.NewGuid();
                    _itemId.Add(parent, parentGuid);
                    _folders.Add(new SlnFolder(parent, parentGuid));
                }

                _hierarchy[currentGuid] = parentGuid;

                if (visited)
                {
                    return;
                }

                currentGuid = parentGuid;
                parent = Directory.GetParent(parent).FullName;

                if (parent.Equals(root, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
        }

        private void BuildHierarchyBottomUp(SlnItem item, string root)
        {
            var parent = Path.Combine(root, item.TargetFolder.TrimEnd(Path.DirectorySeparatorChar));
            var currentGuid = Guid.Empty;

            while (true)
            {
                bool visited = _itemId.TryGetValue(parent, out Guid parentGuid);
                if (!visited)
                {
                    parentGuid = Guid.NewGuid();
                    _itemId.Add(parent, parentGuid);
                    _folders.Add(new SlnFolder(parent, parentGuid));
                }

                if (currentGuid == Guid.Empty)
                {
                    // item hierarchy
                    _itemHierarchy[item] = parentGuid;
                }
                else
                {
                    // folder hierarchy
                    _hierarchy[currentGuid] = parentGuid;
                }

                if (visited)
                {
                    return;
                }

                currentGuid = parentGuid;
                parent = Path.GetDirectoryName(parent);

                if (parent.Equals(root, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
        }
    }
}