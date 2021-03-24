// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Shouldly;
using System;
using System.IO;
using Xunit;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    public class ExtensionMethodsTests
    {
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
        [InlineData(@"C:\RootFolder\SubFolder\MoreSubFolder\LastFolder\SomeFile.txt", @"C:\RootFolder\SubFolder\Sibling\Child\", @"..\..\MoreSubFolder\LastFolder\SomeFile.txt")]
        [InlineData(@"C:\RootFolder\folder1\folder2\SomeFile.txt", @"C:\RootFolder\folder3\folder4\Solution.sln", @"..\..\folder1\folder2\SomeFile.txt")]
        [InlineData(@"C:\folder1\folder2\SomeFile.txt", @"C:\folder3\folder4\folder5\Solution.sln", @"..\..\..\folder1\folder2\SomeFile.txt")]
        [InlineData(@"C:\folder1\SomeFile.txt", @"D:\folder2\Solution.sln", @"C:\folder1\SomeFile.txt")]
        public void Test1(string path, string relativeTo, string expected)
        {
            string actual = path.ToRelativePath(relativeTo);

            actual.ShouldBe(expected);
        }
    }
}