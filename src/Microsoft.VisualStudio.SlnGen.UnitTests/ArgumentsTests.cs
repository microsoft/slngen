// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    public class ArgumentsTests : TestBase
    {
        [Fact]
        public void ExpandWildcards()
        {
            Directory.CreateDirectory(Path.Combine(TestRootPath, "t1", "t2", "t3"));
            Directory.CreateDirectory(Path.Combine(TestRootPath, "t4"));

            string[] projects = new[]
            {
                CreateTempProjectFile("1", string.Empty),
                CreateTempProjectFile("2"),
                CreateTempProjectFile("3", Path.Combine("3", "3B")),
                CreateTempProjectFile("4", Path.Combine("4", "4B", "4C")),
            };

            File.WriteAllText(GetTempFileName(), string.Empty);

            IEnvironmentProvider environmentProvider = new MockEnvironmentProvider
            {
                CurrentDirectory = TestRootPath,
            };

            IEnumerable<string> result = ProgramArguments.ExpandWildcards(environmentProvider, new[] { Path.Combine("**", "*.csproj") }, null, TestRootPath);

            result.ShouldBe(projects, ignoreOrder: true);
        }

        [Fact]
        public void ForwardSlashQuestionMarkDisplaysUsage()
        {
            TestConsole console = new TestConsole();

            int exitCode = Program.Execute(new[] { "/?" }, console, (arguments, console1) => 1);

            exitCode.ShouldBe(0, console.AllOutput);

            console.Output.ShouldContain("Usage: ", Case.Sensitive, console.Output);
        }

        [Fact]
        public void GetConfigurations()
        {
            ProgramArguments arguments = new ProgramArguments
            {
                Configuration = new[] { "One", "Two;Three,one,Four", "four" },
            };

            arguments.GetConfigurations().ShouldBe(new[] { "One", "Two", "Three", "Four" });
        }

        [Fact]
        public void GetPlatforms()
        {
            ProgramArguments arguments = new ProgramArguments()
            {
                Platform = new[] { "Five", "Six;Seven,five,Eight", "eight" },
            };

            arguments.GetPlatforms().ShouldBe(new[] { "Five", "Six", "Seven", "Eight" });
        }

        [Theory]
        [InlineData("--nologo")]
        [InlineData("/nologo")]
        public void NoLogoHidesLogo(string argument)
        {
            TestConsole console = new TestConsole();

            int exitCode = Program.Execute(new[] { argument, "--help" }, console, (arguments, console1) => 0);

            exitCode.ShouldBe(0, console.Output);

            console.OutputLines.First().ShouldStartWith("Usage: ", Case.Sensitive, console.Output);
        }

        [Fact]
        public void ResponseFile()
        {
            TestConsole console = new TestConsole();

            FileInfo responseFile = new FileInfo(Path.Combine(TestRootPath, "response.rsp"));

            File.WriteAllText(responseFile.FullName, "3.csproj 4.csproj\n5.csproj \"6.csproj\" --ignoreMainProject");

            string[] parseProjects = null;
            bool? ignoreMainProject = null;

            int exitCode = Program.Execute(new string[] { "1.csproj", "2.csproj", $"@{responseFile.FullName}" }, console, (arguments, _) =>
            {
                parseProjects = arguments.Projects;
                ignoreMainProject = arguments.IgnoreMainProject;
                return 42;
            });

            exitCode.ShouldBe(42, console.AllOutput);

            parseProjects.ShouldBe(new string[] { "1.csproj", "2.csproj", "3.csproj", "4.csproj", "5.csproj", "6.csproj" });

            ignoreMainProject.ShouldBe(true);
        }

        [Fact]
        public void ExcludePaths()
        {
            Directory.CreateDirectory(Path.Combine(TestRootPath, "dir1"));
            Directory.CreateDirectory(Path.Combine(TestRootPath, "dir2"));
            Directory.CreateDirectory(Path.Combine(TestRootPath, "dir2", "dir3"));

            string[] projects = new[]
            {
                CreateTempProjectFile("1"),
                CreateTempProjectFile("2", Path.Combine(TestRootPath, "dir1")),
                CreateTempProjectFile("3", Path.Combine(TestRootPath, "dir2")),
                CreateTempProjectFile("4", Path.Combine(TestRootPath, "dir2", "dir3")),
            };

            File.WriteAllText(GetTempFileName(), string.Empty);
            IEnvironmentProvider environmentProvider = new MockEnvironmentProvider
            {
                CurrentDirectory = TestRootPath,
            };

            TestConsole console = new TestConsole();
            TestLogger logger = new TestLogger();

            string[] result = null;

            int exitCode = Program.Execute(
                new string[] { Path.Combine("**", "*.csproj"), @"--exclude dir1/2*", @"-e **/dir3" },
                console,
                (arguments, _) =>
                {
                    arguments.TryGetEntryProjectPaths(environmentProvider, logger, out var paths);
                    result = paths.Select(p => Path.GetFileName(p)).ToArray();
                    return 42;
                });

            exitCode.ShouldBe(42, console.AllOutput);

            result.ShouldBe(new string[] { "1.csproj", "3.csproj" });
        }
    }
}