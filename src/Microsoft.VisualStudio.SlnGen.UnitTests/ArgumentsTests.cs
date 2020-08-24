// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Shouldly;
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

            int exitCode = SharedProgram.Main(new[] { "/?" }, console, (arguments, console1) => 1);

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

        [Theory]
        [InlineData("--nologo")]
        [InlineData("/nologo")]
        public void NoLogoHidesLogo(string argument)
        {
            TestConsole console = new TestConsole();

            int exitCode = SharedProgram.Main(new[] { argument, "--help" }, console, (arguments, console1) => 0);

            exitCode.ShouldBe(0, console.Output);

            console.OutputLines.First().ShouldStartWith("Usage: ", console.Output);
        }
    }
}