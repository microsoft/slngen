// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Shouldly;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    public class ArgumentsTests : TestBase
    {
        [Fact]
        public void ForwardSlashQuestionMarkDisplaysUsage()
        {
            TestConsole console = new TestConsole();

            int exitCode = Program.Execute(new[] { "/?" }, console);

            exitCode.ShouldBe(0, console.Output);

            console.Output.ShouldContain("Usage: ", console.Output);
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

        [Fact]
        public void MissingProjectFileLogsErrorAndFails()
        {
            TestConsole console = new TestConsole();

            ProgramArguments programArguments = new ProgramArguments
            {
                Projects = new[]
                {
                    "foo",
                },
            };

            Program program = new Program(programArguments, console, null, "msbuild");

            int exitCode = program.Execute();

            exitCode.ShouldBe(1, console.Output);

            console.Output.ShouldContain($"Project file \"{Path.GetFullPath("foo")}\" does not exist", console.Output);
        }

        [Theory]
        [InlineData("--nologo")]
        [InlineData("/nologo")]
        public void NoLogoHidesLogo(string argument)
        {
            TestConsole console = new TestConsole();

            int exitCode = Program.Execute(new[] { argument, "--help" }, console);

            exitCode.ShouldBe(0, console.Output);

            console.OutputLines.First().ShouldStartWith("Usage: ", console.Output);
        }
    }
}