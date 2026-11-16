// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Shouldly;
using System;
using System.IO;
using Xunit;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    public class DotNetCoreSdkResolverTests : TestBase
    {
        [Fact]
        public void TryResolveDotNetCoreSdk_DoesNotFallBackToDotNetOnPath()
        {
            IEnvironmentProvider environmentProvider = new MockEnvironmentProvider
            {
                CurrentDirectory = TestRootPath,
            };
            FileInfo missingDotNet = new FileInfo(Path.Combine(TestRootPath, "missing-dotnet"));

            bool resolved = DotNetCoreSdkResolver.TryResolveDotNetCoreSdk(
                environmentProvider,
                missingDotNet,
                out DevelopmentEnvironment developmentEnvironment);

            resolved.ShouldBeFalse();
            developmentEnvironment.Success.ShouldBeFalse();
        }
    }
}
