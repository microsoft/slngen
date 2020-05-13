// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    public class SlnHierarchyTests
    {
        [Fact]
        public void HierarchyIsCorrectlyFormed()
        {
            DummyFolder root = DummyFolder.CreateRoot(@"D:\zoo\foo");
            DummyFolder bar = root.AddSubDirectory("bar");
            bar.AddProjectWithDirectory("baz");
            bar.AddProjectWithDirectory("baz1");
            bar.AddProjectWithDirectory("baz2");
            root.AddProjectWithDirectory("bar1");

            List<SlnProject> projects = root.GetAllProjects();

            SlnHierarchy hierarchy = new SlnHierarchy(projects);

            hierarchy.Folders.Select(i => i.FullPath).OrderBy(s => s)
                .ShouldBe(root.GetAllFolders().Select(f => f.FullPath).OrderBy(s => s));

            foreach (SlnProject project in projects)
            {
                hierarchy
                    .Folders
                    .First(i => i.FullPath.Equals(Path.GetDirectoryName(project.FullPath)))
                    .Projects.ShouldHaveSingleItem()
                    .ShouldBe(project);
            }
        }

        [Fact]
        public void HierarchyWithCollapsedFoldersIsCorrectlyFormed()
        {
            DummyFolder root = DummyFolder.CreateRoot(@"D:\zoo\foo");
            DummyFolder bar = root.AddSubDirectory("bar");
            DummyFolder qux = bar.AddSubDirectory("qux");
            SlnProject baz = qux.AddProjectWithDirectory("baz");
            SlnProject baz1 = qux.AddProjectWithDirectory("baz1");
            SlnProject baz2 = qux.AddProjectWithDirectory("baz2");
            SlnProject bar1 = root.AddProjectWithDirectory("bar1");
            SlnProject baz3 = root
                .AddSubDirectory("foo1")
                .AddSubDirectory("foo2")
                .AddProjectWithDirectory("baz3");

            DummyFolder rootExpected = DummyFolder.CreateRoot(@"D:\zoo\foo");
            DummyFolder barQuxExpected = rootExpected.AddSubDirectory($"bar {SlnHierarchy.Separator} qux");
            barQuxExpected.Projects.Add(baz);
            barQuxExpected.Projects.Add(baz1);
            barQuxExpected.Projects.Add(baz2);
            rootExpected.Projects.Add(bar1);
            rootExpected.AddSubDirectory($"foo1 {SlnHierarchy.Separator} foo2 {SlnHierarchy.Separator} baz3").Projects.Add(baz3);

            SlnHierarchy hierarchy = new SlnHierarchy(root.GetAllProjects(), collapseFolders: true);

            SlnFolder[] resultFolders = hierarchy.Folders.OrderBy(f => f.FullPath).ToArray();
            DummyFolder[] expectedFolders = rootExpected.GetAllFolders().OrderBy(f => f.FullPath).ToArray();

            resultFolders.Length.ShouldBe(expectedFolders.Length);

            for (int i = 0; i < resultFolders.Length; i++)
            {
                SlnFolder resultFolder = resultFolders[i];
                DummyFolder expectedFolder = expectedFolders[i];

                resultFolder.Name.ShouldBe(expectedFolder.Name);

                // Verify that expected and results projects match
                resultFolder.Projects.Count.ShouldBe(expectedFolder.Projects.Count);
                resultFolder.Projects.ShouldAllBe(p => expectedFolder.Projects.Contains(p));

                // Verify that expected and results child folders match
                resultFolder.Folders.Count.ShouldBe(expectedFolder.Folders.Count);
                resultFolder.Folders.ShouldAllBe(p => expectedFolder.Folders.Exists(f => f.Name == p.Name));
            }
        }

        private class DummyFolder
        {
            private DummyFolder(string path)
            {
                FileInfo fileInfo = new FileInfo(path);

                this.Folders = new List<DummyFolder>();
                this.Projects = new List<SlnProject>();

                this.FullPath = path;
                this.Name = fileInfo.Name;
            }

            public string FullPath { get; }

            public List<DummyFolder> Folders { get; }

            public List<SlnProject> Projects { get; }

            public string Name { get; }

            public static DummyFolder CreateRoot(string rootPath)
            {
                return new DummyFolder(rootPath);
            }

            public List<SlnProject> GetAllProjects()
            {
                List<SlnProject> projects = this.Folders
                    .SelectMany(f => f.GetAllProjects())
                    .ToList();

                projects.AddRange(this.Projects);

                return projects;
            }

            public List<DummyFolder> GetAllFolders()
            {
                List<DummyFolder> folders = this.Folders
                    .SelectMany(f => f.GetAllFolders())
                    .ToList();

                folders.Add(this);

                return folders;
            }

            public DummyFolder AddSubDirectory(string folderName)
            {
                string path = Path.Combine(this.FullPath, folderName);

                DummyFolder childFolder = new DummyFolder(path);

                this.Folders.Add(childFolder);

                return childFolder;
            }

            public SlnProject AddProject(string name)
            {
                SlnProject project = new SlnProject
                {
                    FullPath = Path.Combine(this.FullPath, name) + ".csproj",
                    Name = name,
                    ProjectGuid = Guid.NewGuid(),
                    ProjectTypeGuid = SlnProject.DefaultLegacyProjectTypeGuid,
                };

                this.Projects.Add(project);

                return project;
            }

            public SlnProject AddProjectWithDirectory(string name)
            {
                return this.AddSubDirectory(name).AddProject(name);
            }
        }
    }
}