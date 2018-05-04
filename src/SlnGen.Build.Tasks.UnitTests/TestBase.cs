// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using Microsoft.Build.Locator;
using System;
using System.IO;

namespace SlnGen.Build.Tasks.UnitTests
{
    public abstract class TestBase
    {
        public static readonly VisualStudioInstance CurrentVisualStudioInstance = MSBuildLocator.RegisterDefaults();

        private readonly string _testRootPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        protected TestBase()
        {
            MSBuildPath = CurrentVisualStudioInstance.MSBuildPath;
        }

        public string TestRootPath
        {
            get
            {
                Directory.CreateDirectory(_testRootPath);
                return _testRootPath;
            }
        }

        protected string MSBuildPath { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Directory.Exists(TestRootPath))
                {
                    Directory.Delete(TestRootPath, recursive: true);
                }
            }
        }

        protected string GetTempFileName(string extension = null)
        {
            Directory.CreateDirectory(TestRootPath);

            return Path.Combine(TestRootPath, $"{Path.GetRandomFileName()}{extension ?? String.Empty}");
        }
    }
}