// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Shouldly;
using System.IO;
using Xunit;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    public class SlnGenFrameworkTests : TestBase
    {
        [Theory]
        [InlineData("10", "net10.0")]
        [InlineData("11", "net11.0")]
        public void SelectsFrameworkMatchingSdkRegardlessOfCurrentRuntime(string sdkMajorVersion, string expectedFramework)
        {
            string slnGenDirectory = CreateSlnGenDirectory("net472", "net10.0", "net11.0");

            SlnGenFramework.TryGetDotNetFramework(sdkMajorVersion, slnGenDirectory, out string framework).ShouldBe(SlnGenFrameworkResult.Success);

            framework.ShouldBe(expectedFramework);
        }

        [Fact]
        public void NewerSdkWithoutMatchingBuildIsNotAvailable()
        {
            string slnGenDirectory = CreateSlnGenDirectory("net472", "net10.0", "net11.0");

            SlnGenFramework.TryGetDotNetFramework("12", slnGenDirectory, out string framework).ShouldBe(SlnGenFrameworkResult.SdkVersionNotAvailable);

            framework.ShouldBeNull();
        }

        [Fact]
        public void NewerSdkWithMatchingBuildIsSelected()
        {
            string slnGenDirectory = CreateSlnGenDirectory("net472", "net10.0", "net11.0", "net12.0");

            SlnGenFramework.TryGetDotNetFramework("12", slnGenDirectory, out string framework).ShouldBe(SlnGenFrameworkResult.Success);

            framework.ShouldBe("net12.0");
        }

        [Fact]
        public void OlderSdkIsUnsupported()
        {
            string slnGenDirectory = CreateSlnGenDirectory("net472", "net10.0", "net11.0");

            SlnGenFramework.TryGetDotNetFramework("9", slnGenDirectory, out string framework).ShouldBe(SlnGenFrameworkResult.UnsupportedSdkVersion);

            framework.ShouldBeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("-10")]
        public void InvalidSdkVersionIsUnknown(string sdkMajorVersion)
        {
            string slnGenDirectory = CreateSlnGenDirectory("net472", "net10.0", "net11.0");

            SlnGenFramework.TryGetDotNetFramework(sdkMajorVersion, slnGenDirectory, out string framework).ShouldBe(SlnGenFrameworkResult.UnknownSdkVersion);

            framework.ShouldBeNull();
        }

        private string CreateSlnGenDirectory(params string[] frameworks)
        {
            string slnGenDirectory = Path.Combine(TestRootPath, "slngen");

            foreach (string framework in frameworks)
            {
                Directory.CreateDirectory(Path.Combine(slnGenDirectory, framework));
            }

            return slnGenDirectory;
        }
    }
}
