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
        private readonly string _driveRoot = Path.GetPathRoot(Environment.CurrentDirectory);

        [Fact]
        public void HierarchyIgnoresCase()
        {
            SlnProject[] projects = new[]
            {
                new SlnProject
                {
                    FullPath = Path.Combine(_driveRoot, "Code", "ProjectA", "ProjectA.csproj"),
                    Name = "ProjectA",
                    ProjectGuid = Guid.NewGuid(),
                    ProjectTypeGuid = SlnProject.DefaultLegacyProjectTypeGuid,
                },
                new SlnProject
                {
                    FullPath = Path.Combine(_driveRoot, "code", "ProjectB", "ProjectB.csproj"),
                    Name = "ProjectB",
                    ProjectGuid = Guid.NewGuid(),
                    ProjectTypeGuid = SlnProject.DefaultLegacyProjectTypeGuid,
                },
            };

            SlnHierarchy hierarchy = SlnHierarchy.CreateFromProjectDirectories(projects);

            GetFolderStructureAsString(hierarchy.Folders).ShouldBe(
                $@"{_driveRoot}Code - Code
{_driveRoot}Code{Path.DirectorySeparatorChar}ProjectA - ProjectA
{_driveRoot}code{Path.DirectorySeparatorChar}ProjectB - ProjectB",
                StringCompareShould.IgnoreLineEndings);
        }

        [Fact]
        public void HierarchyIsCorrectlyFormed()
        {
            SlnProject[] projects =
            {
                new SlnProject
                {
                    FullPath = $"{Path.Combine(_driveRoot, "zoo", "foo", "bar", "baz", "baz")}.csproj",
                    Name = "baz",
                    ProjectGuid = Guid.NewGuid(),
                    ProjectTypeGuid = SlnProject.DefaultLegacyProjectTypeGuid,
                },
                new SlnProject
                {
                    FullPath = $"{Path.Combine(_driveRoot, "zoo", "foo", "bar", "baz1", "baz1")}.csproj",
                    Name = "baz1",
                    ProjectGuid = Guid.NewGuid(),
                    ProjectTypeGuid = SlnProject.DefaultLegacyProjectTypeGuid,
                },
                new SlnProject
                {
                    FullPath = $"{Path.Combine(_driveRoot, "zoo", "foo", "bar", "baz2", "baz2")}.csproj",
                    Name = "baz2",
                    ProjectGuid = Guid.NewGuid(),
                    ProjectTypeGuid = SlnProject.DefaultLegacyProjectTypeGuid,
                },
                new SlnProject
                {
                    FullPath = $"{Path.Combine(_driveRoot, "zoo", "foo", "bar1", "bar1")}.csproj",
                    Name = "bar1",
                    ProjectGuid = Guid.NewGuid(),
                    ProjectTypeGuid = SlnProject.DefaultLegacyProjectTypeGuid,
                },
            };

            SlnHierarchy hierarchy = SlnHierarchy.CreateFromProjectDirectories(projects);

            GetFolderStructureAsString(hierarchy.Folders).ShouldBe(
                $@"{_driveRoot}zoo{Path.DirectorySeparatorChar}foo - foo
{_driveRoot}zoo{Path.DirectorySeparatorChar}foo{Path.DirectorySeparatorChar}bar - bar
{_driveRoot}zoo{Path.DirectorySeparatorChar}foo{Path.DirectorySeparatorChar}bar{Path.DirectorySeparatorChar}baz - baz
{_driveRoot}zoo{Path.DirectorySeparatorChar}foo{Path.DirectorySeparatorChar}bar{Path.DirectorySeparatorChar}baz1 - baz1
{_driveRoot}zoo{Path.DirectorySeparatorChar}foo{Path.DirectorySeparatorChar}bar{Path.DirectorySeparatorChar}baz2 - baz2
{_driveRoot}zoo{Path.DirectorySeparatorChar}foo{Path.DirectorySeparatorChar}bar1 - bar1",
                StringCompareShould.IgnoreLineEndings);
        }

        [Fact]
        public void SingleFolderWithProjectsShouldNotCollapseIntoParentFolderWithProjects()
        {
            SlnProject[] projects =
            {
                new SlnProject
                {
                    FullPath = $"{Path.Combine(_driveRoot, "foo", "src", "subfolder", "project1", "project1")}.csproj",
                    Name = "project1",
                    ProjectGuid = Guid.NewGuid(),
                    ProjectTypeGuid = SlnProject.DefaultLegacyProjectTypeGuid,
                },
                new SlnProject
                {
                    FullPath = $"{Path.Combine(_driveRoot, "foo", "src", "subfolder", "project1Tests", "project1Tests")}.csproj",
                    Name = "project1Tests",
                    ProjectGuid = Guid.NewGuid(),
                    ProjectTypeGuid = SlnProject.DefaultLegacyProjectTypeGuid,
                },
                new SlnProject
                {
                    FullPath = $"{Path.Combine(_driveRoot, "foo", "src", "project2", "project2")}.csproj",
                    Name = "project2",
                    ProjectGuid = Guid.NewGuid(),
                    ProjectTypeGuid = SlnProject.DefaultLegacyProjectTypeGuid,
                },
                new SlnProject
                {
                    FullPath = $"{Path.Combine(_driveRoot, "foo", "project3", "project3")}.csproj",
                    Name = "project3",
                    ProjectGuid = Guid.NewGuid(),
                    ProjectTypeGuid = SlnProject.DefaultLegacyProjectTypeGuid,
                },
            };

            SlnHierarchy hierarchy = SlnHierarchy.CreateFromProjectDirectories(projects, collapseFolders: true);

            GetFolderStructureAsString(hierarchy.Folders).ShouldBe(
                $@"{_driveRoot}foo - foo
{_driveRoot}foo{Path.DirectorySeparatorChar}src - src
{_driveRoot}foo{Path.DirectorySeparatorChar}src{Path.DirectorySeparatorChar}subfolder - subfolder",
                StringCompareShould.IgnoreLineEndings);
        }

        [Fact]
        public void HierarchyWithMultipleSubfoldersUnderACollapsedFolderIsCorrectlyFormed()
        {
            SlnProject[] projects =
            {
                new SlnProject
                {
                    FullPath = $"{Path.Combine(_driveRoot, "foo", "src", "subfolder", "src", "project1", "project1")}.csproj",
                    Name = "project1",
                    ProjectGuid = Guid.NewGuid(),
                    ProjectTypeGuid = SlnProject.DefaultLegacyProjectTypeGuid,
                },
                new SlnProject
                {
                    FullPath = $"{Path.Combine(_driveRoot, "foo", "src", "subfolder", "tests", "project1Tests", "project1Tests")}.csproj",
                    Name = "project1Tests",
                    ProjectGuid = Guid.NewGuid(),
                    ProjectTypeGuid = SlnProject.DefaultLegacyProjectTypeGuid,
                },
                new SlnProject
                {
                    FullPath = $"{Path.Combine(_driveRoot, "foo", "project2", "project2")}.csproj",
                    Name = "project2",
                    ProjectGuid = Guid.NewGuid(),
                    ProjectTypeGuid = SlnProject.DefaultLegacyProjectTypeGuid,
                },
            };

            SlnHierarchy hierarchy = SlnHierarchy.CreateFromProjectDirectories(projects, collapseFolders: true);

            GetFolderStructureAsString(hierarchy.Folders).ShouldBe(
                $@"{_driveRoot}foo - foo
{_driveRoot}foo{Path.DirectorySeparatorChar}src - src {SlnHierarchy.Separator} subfolder
{_driveRoot}foo{Path.DirectorySeparatorChar}src{Path.DirectorySeparatorChar}subfolder{Path.DirectorySeparatorChar}src - src
{_driveRoot}foo{Path.DirectorySeparatorChar}src{Path.DirectorySeparatorChar}subfolder{Path.DirectorySeparatorChar}tests - tests",
                StringCompareShould.IgnoreLineEndings);
        }

        [Fact]
        public void HierarchyWithCollapsedFoldersIsCorrectlyFormed()
        {
            SlnProject[] projects =
            {
                new SlnProject
                {
                    FullPath = $"{Path.Combine(_driveRoot, "zoo", "foo", "bar", "qux", "baz", "baz")}.csproj",
                    Name = "baz",
                    ProjectGuid = Guid.NewGuid(),
                    ProjectTypeGuid = SlnProject.DefaultLegacyProjectTypeGuid,
                },
                new SlnProject
                {
                    FullPath = $"{Path.Combine(_driveRoot, "zoo", "foo", "bar", "qux", "baz1", "baz1")}.csproj",
                    Name = "baz1",
                    ProjectGuid = Guid.NewGuid(),
                    ProjectTypeGuid = SlnProject.DefaultLegacyProjectTypeGuid,
                },
                new SlnProject
                {
                    FullPath = $"{Path.Combine(_driveRoot, "zoo", "foo", "bar", "qux", "baz2", "baz2")}.csproj",
                    Name = "baz2",
                    ProjectGuid = Guid.NewGuid(),
                    ProjectTypeGuid = SlnProject.DefaultLegacyProjectTypeGuid,
                },
                new SlnProject
                {
                    FullPath = $"{Path.Combine(_driveRoot, "zoo", "foo", "foo1", "foo2", "baz3", "baz3")}.csproj",
                    Name = "baz3",
                    ProjectGuid = Guid.NewGuid(),
                    ProjectTypeGuid = SlnProject.DefaultLegacyProjectTypeGuid,
                },
            };

            SlnHierarchy hierarchy = SlnHierarchy.CreateFromProjectDirectories(projects, collapseFolders: true);

            GetFolderStructureAsString(hierarchy.Folders).ShouldBe(
                $@"{_driveRoot}zoo{Path.DirectorySeparatorChar}foo - foo
{_driveRoot}zoo{Path.DirectorySeparatorChar}foo{Path.DirectorySeparatorChar}bar - bar {SlnHierarchy.Separator} qux
{_driveRoot}zoo{Path.DirectorySeparatorChar}foo{Path.DirectorySeparatorChar}foo1 - foo1 {SlnHierarchy.Separator} foo2",
                StringCompareShould.IgnoreLineEndings);
        }

        private static string GetFolderStructureAsString(IEnumerable<SlnFolder> folders)
        {
            return string.Join(
                Environment.NewLine,
                folders.OrderBy(i => i.FullPath).Select(i => $"{i.FullPath} - {i.Name}"));
        }
    }
}