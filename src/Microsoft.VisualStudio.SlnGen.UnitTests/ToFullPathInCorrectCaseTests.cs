// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Shouldly;
using System;
using System.IO;
using Xunit;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    public class ToFullPathInCorrectCaseTests
    {
        [Fact]
        public void IncorrectCaseInDirectory()
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
        public void IncorrectCaseInFile()
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
    }
}