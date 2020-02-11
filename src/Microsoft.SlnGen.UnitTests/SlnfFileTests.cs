// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Shouldly;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Microsoft.SlnGen.UnitTests
{
    public class SlnfFileTests
    {
        [Fact]
        public void Simple()
        {
            SlnfFile slnfFile = new SlnfFile
            {
                SolutionFilePath = @"..\..\..\foo.sln",
                Projects = new List<string>
                {
                    @"src\foo\bar\bar.csproj",
                    @"src\foo\baz\baz.csproj",
                },
            };

            using (MemoryStream stream = new MemoryStream())
            {
                slnfFile.Save(stream);

                string actual = Encoding.UTF8.GetString(stream.ToArray());

                actual.ShouldBe(
                    @"{
  ""solution"": {
    ""path"": ""..\\..\\..\\foo.sln"",
    ""projects"": [
      ""src\\foo\\bar\\bar.csproj"",
      ""src\\foo\\baz\\baz.csproj""
    ]
  }
}",
                    StringCompareShould.IgnoreLineEndings);
            }
        }
    }
}