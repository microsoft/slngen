// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Shouldly;
using System;
using Xunit;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    public class GitRepositoryInfoTests
    {
        [Theory]
        [InlineData("username:password@")]
        [InlineData("username@")]
        [InlineData(":password@")]
        [InlineData("@")]
        public void StripUsernameAndPassword(string userInfo)
        {
            Uri before = new Uri($"https://{userInfo}github.com/organization/repo.git");

            Uri after = GitRepositoryInfo.StripUsernameAndPassword(before);

            after.UserInfo.ShouldBeEmpty();
        }

        [Fact]
        public void StripUsernameAndPasswordDoesNothingIfNoUserInfo()
        {
            Uri before = new Uri("https://github.com/organization/repo.git");

            Uri after = GitRepositoryInfo.StripUsernameAndPassword(before);

            after.ShouldBeSameAs(before);
        }
    }
}