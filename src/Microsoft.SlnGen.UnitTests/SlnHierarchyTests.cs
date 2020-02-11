// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.SlnGen.UnitTests
{
    public class SlnHierarchyTests
    {
        [Fact]
        public void HierarchyIsCorrectlyFormed()
        {
            List<SlnProject> projects = new List<SlnProject>
            {
                new SlnProject
                {
                    FullPath = @"D:\zoo\foo\bar\baz\baz.csproj",
                    Name = "baz",
                    ProjectGuid = Guid.NewGuid(),
                    ProjectTypeGuid = SlnProject.DefaultLegacyProjectTypeGuid,
                },
                new SlnProject
                {
                    FullPath = @"D:\zoo\foo\bar\baz1\baz1.csproj",
                    Name = "baz1",
                    ProjectGuid = Guid.NewGuid(),
                    ProjectTypeGuid = SlnProject.DefaultLegacyProjectTypeGuid,
                },
                new SlnProject
                {
                    FullPath = @"D:\zoo\foo\bar\baz2\baz2.csproj",
                    Name = "baz2",
                    ProjectGuid = Guid.NewGuid(),
                    ProjectTypeGuid = SlnProject.DefaultLegacyProjectTypeGuid,
                },
                new SlnProject
                {
                    FullPath = @"D:\zoo\foo\bar1\bar1.csproj",
                    Name = "bar1",
                    ProjectGuid = Guid.NewGuid(),
                    ProjectTypeGuid = SlnProject.DefaultLegacyProjectTypeGuid,
                },
            };

            SlnHierarchy hierarchy = new SlnHierarchy(projects);

            hierarchy.Folders.Select(i => i.FullPath)
                .ShouldBe(new[]
                {
                    @"D:\zoo\foo\bar\baz",
                    @"D:\zoo\foo\bar\baz1",
                    @"D:\zoo\foo\bar\baz2",
                    @"D:\zoo\foo\bar",
                    @"D:\zoo\foo\bar1",
                    @"D:\zoo\foo",
                });

            foreach (SlnProject project in projects)
            {
                hierarchy
                    .Folders
                    .First(i => i.FullPath.Equals(Path.GetDirectoryName(project.FullPath)))
                    .Projects.ShouldHaveSingleItem()
                    .ShouldBe(project);
            }
        }
    }
}