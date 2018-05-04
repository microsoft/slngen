// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using NUnit.Framework;
using Shouldly;
using System;
using System.IO;

namespace SlnGen.Build.Tasks.UnitTests
{
    [TestFixture]
    public class ToFullPathInCorrectCaseTests
    {
        [Test]
        public void IncorrectCaseInDirectory()
        {
            ValidatePath(Path.GetTempFileName());
        }

        [Test]
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