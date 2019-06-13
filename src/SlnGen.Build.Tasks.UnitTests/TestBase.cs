// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using Microsoft.Build.Utilities.ProjectCreation;
using System;
using System.IO;
using System.Reflection;

namespace SlnGen.Build.Tasks.UnitTests
{
    public abstract class TestBase : MSBuildTestBase
    {
        private readonly string _testRootPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        public TestBase()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (string.Equals(args.Name, "Microsoft.Build.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"))
                {
                    return Assembly.LoadFrom(Path.Combine(MSBuildAssemblyResolver.MSBuildPath, "Microsoft.Build.Framework.dll"));
                }

                return null;
            };
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

            return Path.Combine(TestRootPath, $"{Path.GetRandomFileName()}{extension ?? string.Empty}");
        }
    }
}