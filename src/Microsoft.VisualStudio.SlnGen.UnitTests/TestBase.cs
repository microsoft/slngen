// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Utilities.ProjectCreation;
using System;
using System.IO;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    public abstract class TestBase : MSBuildTestBase
    {
        private readonly string _testRootPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        public string TestRootPath
        {
            get
            {
                Directory.CreateDirectory(_testRootPath);
                return _testRootPath;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected string CreateTempProjectFile(string name, string directoryPath = default)
        {
            string filePath = GetTempProjectFile(name, directoryPath);

            File.WriteAllText(filePath, "<Project />");

            return filePath;
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

            return Path.Combine(TestRootPath, $"{Path.GetRandomFileName()}{extension ?? string.Empty}");
        }

        protected string GetTempProjectFile(string name, string directoryPath = default)
        {
            DirectoryInfo projectDirectory = Directory.CreateDirectory(Path.Combine(TestRootPath, directoryPath ?? name));

            return Path.Combine(projectDirectory.FullName, $"{name}.csproj");
        }
    }
}