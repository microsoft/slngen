// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Shouldly;
using System;
using Xunit;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    public class MSBuildFeatureFlagsTests
    {
        [Theory]
        [InlineData("1", true)]
        [InlineData("0", false)]
        [InlineData(null, false)]
        public void UseSimpleProjectRootElementCacheConcurrency_ReflectsEnvironmentVariable(string value, bool expected)
        {
            MockEnvironmentProvider environmentProvider = new MockEnvironmentProvider();
            environmentProvider.SetEnvironmentVariable("MSBUILDUSESIMPLEPROJECTROOTELEMENTCACHECONCURRENCY", value);

            MSBuildFeatureFlags featureFlags = new MSBuildFeatureFlags(environmentProvider);

            featureFlags.UseSimpleProjectRootElementCacheConcurrency.ShouldBe(expected);
        }
    }
}
