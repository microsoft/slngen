// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Utilities.ProjectCreation;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    public class ExtensionMethodsTests : TestBase
    {
        public static IEnumerable<object[]> GetRelativePathData()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                yield return new object[]
                {
                    @"C:\RootFolder\SubFolder\MoreSubFolder\LastFolder\SomeFile.txt",
                    @"C:\RootFolder\SubFolder\Sibling\Child\",
                    @"..\..\MoreSubFolder\LastFolder\SomeFile.txt",
                };

                yield return new object[]
                {
                    @"C:\RootFolder\folder1\folder2\SomeFile.txt",
                    @"C:\RootFolder\folder3\folder4\Solution.sln",
                    @"..\..\folder1\folder2\SomeFile.txt",
                };

                yield return new object[]
                {
                    @"C:\folder1\folder2\SomeFile.txt",
                    @"C:\folder3\folder4\folder5\Solution.sln",
                    @"..\..\..\folder1\folder2\SomeFile.txt",
                };

                yield return new object[]
                {
                    @"C:\folder1\SomeFile.txt",
                    @"D:\folder2\Solution.sln",
                    @"C:\folder1\SomeFile.txt",
                };
            }
            else
            {
                yield return new object[]
                {
                    @"/RootFolder/SubFolder/MoreSubFolder/LastFolder/SomeFile.txt",
                    @"/RootFolder/SubFolder/Sibling/Child/",
                    @"../../MoreSubFolder/LastFolder/SomeFile.txt",
                };

                yield return new object[]
                {
                    @"/RootFolder/folder1/folder2/SomeFile.txt",
                    @"/RootFolder/folder3/folder4/Solution.sln",
                    @"../../folder1/folder2/SomeFile.txt",
                };

                yield return new object[]
                {
                    @"/folder1/folder2/SomeFile.txt",
                    @"/folder3/folder4/folder5/Solution.sln",
                    @"../../../folder1/folder2/SomeFile.txt",
                };
            }
        }

        [Fact]
        public void GetConditionedPropertyValuesIsEmpty()
        {
            Project project = ProjectCreator.Create()
                .Save(GetTempFileName(ProjectFileExtensions.Cpp));
            project.GetConditionedPropertyValues("Foo")
                .ShouldBeEquivalentTo(Enumerable.Empty<string>());
        }

        [Fact]
        public void GetConditionedPropertyValues()
        {
            Project project = ProjectCreator.Create()
                .PropertyGroup(condition: " '$(Foo)' == 'Bar' ")
                .Save(GetTempFileName(ProjectFileExtensions.FSharp));
            project.GetConditionedPropertyValues("Foo")
                .ToList()
                .ShouldBeEquivalentTo(new List<string> { "Bar" });
        }

        [Fact]
        public void GetPossiblePropertyValuesDirectly()
        {
            Project project = ProjectCreator.Create()
                .Property("Foo", "Bar")
                .Save(GetTempFileName(ProjectFileExtensions.FSharp));
            project.GetPossiblePropertyValuesOrDefault("Foo", "Baz")
                .ToList()
                .ShouldBeEquivalentTo(new List<string> { "Bar" });
        }

        [Fact]
        public void GetPossiblePropertyValuesFromConditional()
        {
            Project project = ProjectCreator.Create()
                .PropertyGroup(condition: " '$(Platform)' == 'x86' ")
                .PropertyGroup(condition: " '$(Platform)' == 'arm64' ")
                .Save(GetTempFileName(ProjectFileExtensions.CSharp));
            project.GetPossiblePropertyValuesOrDefault("Platform", "AnyCPU")
                .ToList()
                .ShouldBeEquivalentTo(new List<string> { "x86", "arm64" });
        }

        [Fact]
        public void GetPossiblePropertyValuesReturnsDefault()
        {
            Project project = ProjectCreator.Create()
                .Save(GetTempFileName(ProjectFileExtensions.FSharp));
            project.GetPossiblePropertyValuesOrDefault("Foo", "Baz")
                .ToList()
                .ShouldBeEquivalentTo(new List<string> { "Baz" });
        }

#if NETFRAMEWORK
        [Fact]
        public void ToFullPathInCorrectCaseDirectory()
        {
            const string filename = "dca96b5e957449c4973f8fbb72c33e29.txt";

            DirectoryInfo directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
            try
            {
                File.WriteAllText(Path.Combine(directory.FullName, filename), string.Empty);

                Path.Combine(directory.FullName.ToUpperInvariant(), filename)
                    .ToFullPathInCorrectCase()
                    .ShouldBe(Path.Combine(directory.FullName, filename));
            }
            finally
            {
                directory.Delete(recursive: true);
            }
        }

        [Fact]
        public void ToFullPathInCorrectCaseFile()
        {
            const string filename = "dca96b5e957449c4973f8fbb72c33e29.txt";

            DirectoryInfo directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
            try
            {
                File.WriteAllText(Path.Combine(directory.FullName, filename), string.Empty);

                Path.Combine(directory.FullName, filename.ToUpperInvariant())
                    .ToFullPathInCorrectCase()
                    .ShouldBe(Path.Combine(directory.FullName, filename));
            }
            finally
            {
                directory.Delete(recursive: true);
            }
        }
#endif

        [Theory]
        [MemberData(nameof(GetRelativePathData))]
        public void ToRelativePath(string path, string relativeTo, string expected)
        {
            string actual = path.ToRelativePath(relativeTo);

            actual.ShouldBe(expected);
        }
    }
}