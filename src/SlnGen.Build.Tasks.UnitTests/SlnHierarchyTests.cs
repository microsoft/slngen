// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using Shouldly;
using SlnGen.Build.Tasks.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace SlnGen.Build.Tasks.UnitTests
{
    public class SlnHierarchyTests
    {
        [Fact]
        public void HierarchyIsCorrectlyFormed()
        {
            string[] configurations = { "Debug" };
            string[] platforms = { "AnyCPU" };

            List<SlnProject> projects = new List<SlnProject>
            {
                new SlnProject(@"D:\foo\bar\baz\baz.csproj", "baz", Guid.NewGuid(), SlnProject.DefaultLegacyProjectTypeGuid, configurations, platforms, false, isDeployable: false),
                new SlnProject(@"D:\foo\bar\baz1\baz1.csproj", "baz1", Guid.NewGuid(), SlnProject.DefaultLegacyProjectTypeGuid, configurations, platforms, false, isDeployable: false),
                new SlnProject(@"D:\foo\bar\baz2\baz2.csproj", "baz2", Guid.NewGuid(), SlnProject.DefaultLegacyProjectTypeGuid, configurations, platforms, false, isDeployable: false),
                new SlnProject(@"D:\foo\bar1\bar1.csproj", "bar1", Guid.NewGuid(), SlnProject.DefaultLegacyProjectTypeGuid, configurations, platforms, false, isDeployable: false)
            };

            SlnHierarchy hierarchy = new SlnHierarchy(projects);

            hierarchy.Folders.Select(i => i.FullPath)
                .ShouldBe(new[]
                {
                    @"D:\foo\bar\baz",
                    @"D:\foo\bar\baz1",
                    @"D:\foo\bar\baz2",
                    @"D:\foo\bar",
                    @"D:\foo\bar1",
                    @"D:\foo"
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