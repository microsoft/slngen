// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Shouldly;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    public class ArgumentsTests
    {
        [Fact]
        public void ForwardSlashQuestionMarkDisplaysUsage()
        {
            TestConsole console = new TestConsole();

            Program.Console = console;

            int exitCode = Program.Main(new[] { "/?" });

            exitCode.ShouldBe(0, console.Output);

            console.Output.ShouldContain("Usage: slngen", console.Output);
        }

        [Theory]
        [InlineData("--nologo")]
        [InlineData("/nologo")]
        public void NoLogoHidesLogo(string argument)
        {
            TestConsole console = new TestConsole();

            Program.Console = console;

            int exitCode = Program.Main(new[] { argument, "--help" });

            exitCode.ShouldBe(0, console.Output);

            console.OutputLines.First().ShouldStartWith("Usage: slngen", console.Output);
        }
    }
}