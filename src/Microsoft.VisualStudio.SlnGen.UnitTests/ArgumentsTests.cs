// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Shouldly;
using System;
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

#if !NETFRAMEWORK
        [Fact]
        public void ExpandWildcards()
        {
            var root = Path.GetTempPath();
            var work = Directory.CreateDirectory(Path.Combine(root, "work" + Environment.ProcessId.ToString()));

            try
            {
                // directory tree
                _ = Directory.CreateDirectory(Path.Combine(work.FullName, "t1"));
                _ = Directory.CreateDirectory(Path.Combine(work.FullName, "t1", "t2"));
                _ = Directory.CreateDirectory(Path.Combine(work.FullName, "t1", "t2", "t3"));
                _ = Directory.CreateDirectory(Path.Combine(work.FullName, "t4"));

                // sprinkle some files around
                File.WriteAllText(Path.Combine(work.FullName, "1.csproj"), string.Empty);
                File.WriteAllText(Path.Combine(work.FullName, "2.csproj"), string.Empty);
                File.WriteAllText(Path.Combine(work.FullName, "XXX"), string.Empty);
                File.WriteAllText(Path.Combine(work.FullName, "t1", "3.csproj"), string.Empty);
                File.WriteAllText(Path.Combine(work.FullName, "t1", "XXX"), string.Empty);
                File.WriteAllText(Path.Combine(work.FullName, "t1", "t2", "4.csproj"), string.Empty);
                File.WriteAllText(Path.Combine(work.FullName, "t1", "t2", "5.csproj"), string.Empty);
                File.WriteAllText(Path.Combine(work.FullName, "t1", "t2", "t3", "6.csproj"), string.Empty);

                var old = Environment.CurrentDirectory;
                Environment.CurrentDirectory = work.FullName;
                var result = ProgramArguments.ExpandWildcards(new[] { "**\\*.csproj" });
                Environment.CurrentDirectory = old;

                var files = result.Select(x => Path.GetFileName(x)).ToArray();
                Array.Sort(files);

                Assert.Equal(6, files.Length);
                for (int i = 0; i < files.Length; i++)
                {
                    Assert.Equal(files[i], $"{i + 1}.csproj");
                }
            }
            finally
            {
                Directory.Delete(work.FullName, true);
            }
        }
#endif
    }
}