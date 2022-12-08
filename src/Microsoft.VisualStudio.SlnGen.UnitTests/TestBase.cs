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
        private const string DotNetSdkVersion =
#if NETCOREAPP3_1
                "3.1.0";
#elif NET5_0
                "5.0.0";
#elif NET6_0
                "6.0.0";
#elif NET7_0 || NETFRAMEWORK
                "7.0.0";
#elif NET8_0
                "8.0.0";
#else
                Unknown target framework
#endif

        private readonly DirectoryInfo _testRootPath;

        protected TestBase()
        {
            _testRootPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

            File.WriteAllText(
                Path.Combine(TestRootPath, "global.json"),
                $@"{{
  ""sdk"": {{
    ""version"": ""{DotNetSdkVersion}"",
    ""rollForward"": ""latestMinor""
  }}
}}");
        }

        public string TestRootPath
        {
            get
            {
                return _testRootPath.FullName;
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
                if (_testRootPath.Exists)
                {
                    _testRootPath.Delete(recursive: true);
                }
            }
        }

        protected string GetTempFileName(string extension = null)
        {
            return Path.Combine(TestRootPath, $"{Path.GetRandomFileName()}{extension ?? string.Empty}");
        }

        protected string GetTempProjectFile(string name, string directoryPath = default)
        {
            DirectoryInfo projectDirectory = Directory.CreateDirectory(Path.Combine(TestRootPath, directoryPath ?? name));

            return Path.Combine(projectDirectory.FullName, $"{name}.csproj");
        }
    }
}