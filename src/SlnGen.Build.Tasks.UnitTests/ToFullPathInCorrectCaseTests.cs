// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using Shouldly;
using System;
using System.IO;
using Xunit;

namespace SlnGen.Build.Tasks.UnitTests
{
    public class ToFullPathInCorrectCaseTests
    {
        [Fact]
        public void IncorrectCaseInDirectory()
        {
            ValidatePath(Path.GetTempFileName());
        }

        [Fact]
        public void IncorrectCaseInFile()
        {
            string expectedPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid().ToString("N").ToUpperInvariant()}.txt");

            File.WriteAllText(expectedPath, String.Empty);

            ValidatePath(expectedPath);
        }

        private void ValidatePath(string expectedPath)
        {
            try
            {
                expectedPath
                    .ToLowerInvariant()
                    .ToFullPathInCorrectCase()
                    .ShouldBe(expectedPath);
            }
            finally
            {
                File.Delete(expectedPath);
            }
        }
    }
}