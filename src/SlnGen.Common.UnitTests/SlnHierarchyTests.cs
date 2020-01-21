// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace SlnGen.Common.UnitTests
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
                new SlnProject(@"D:\zoo\foo\bar\baz\baz.csproj", "baz", Guid.NewGuid(), SlnProject.DefaultLegacyProjectTypeGuid, configurations, platforms, false, isDeployable: false),
                new SlnProject(@"D:\zoo\foo\bar\baz1\baz1.csproj", "baz1", Guid.NewGuid(), SlnProject.DefaultLegacyProjectTypeGuid, configurations, platforms, false, isDeployable: false),
                new SlnProject(@"D:\zoo\foo\bar\baz2\baz2.csproj", "baz2", Guid.NewGuid(), SlnProject.DefaultLegacyProjectTypeGuid, configurations, platforms, false, isDeployable: false),
                new SlnProject(@"D:\zoo\foo\bar1\bar1.csproj", "bar1", Guid.NewGuid(), SlnProject.DefaultLegacyProjectTypeGuid, configurations, platforms, false, isDeployable: false),
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