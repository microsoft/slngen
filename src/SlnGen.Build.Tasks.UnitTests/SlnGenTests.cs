// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using Microsoft.Build.Framework;
using Shouldly;
using SlnGen.Build.Tasks.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace SlnGen.Build.Tasks.UnitTests
{
    public sealed class SlnGenTests : TestBase
    {
        [Fact]
        public void ParseCustomProjectTypeGuidsDeduplicatesList()
        {
            Guid expectedProjectTypeGuid = new Guid("C139C737-2894-46A0-B1EB-DDD052FD8DCB");

            ITaskItem[] customProjectTypeGuids =
            {
                new MockTaskItem(" .foo ")
                {
                    { SlnGen.CustomProjectTypeGuidMetadataName, "1AB09E1B-77F6-4982-B020-374DB9DF2BD2" }
                },
                new MockTaskItem(".foo")
                {
                    { SlnGen.CustomProjectTypeGuidMetadataName, expectedProjectTypeGuid.ToString() }
                },
            };

            ValidateParseCustomProjectTypeGuids(customProjectTypeGuids, ".foo", expectedProjectTypeGuid);
        }

        [Fact]
        public void ParseCustomProjectTypeGuidsFormatsFileExtensionAndGuid()
        {
            ValidateParseCustomProjectTypeGuids(
                " .FoO  ",
                "  9d9339782d2a4fb2b72d8746d88e73b7 ",
                ".foo",
                new Guid("9D933978-2D2A-4FB2-B72D-8746D88E73B7"));
        }

        [Fact]
        public void ParseCustomProjectTypeGuidsIgnoresNonFileExtensions()
        {
            Guid expectedProjectTypeGuid = new Guid("C139C737-2894-46A0-B1EB-DDD052FD8DCB");

            ITaskItem[] customProjectTypeGuids =
            {
                new MockTaskItem("foo")
                {
                    { SlnGen.CustomProjectTypeGuidMetadataName, "9d933978-2d2a-4fb2-b72d-8746d88e73b7" }
                },
                new MockTaskItem(".foo")
                {
                    { SlnGen.CustomProjectTypeGuidMetadataName, expectedProjectTypeGuid.ToString() }
                },
            };

            ValidateParseCustomProjectTypeGuids(customProjectTypeGuids, ".foo", expectedProjectTypeGuid);
        }

        [Fact]
        public void SolutionItems()
        {
            Dictionary<string, SlnItem> solutionItems = new Dictionary<string, SlnItem>
            {
                { "foo", new SlnItem(Path.GetFullPath("foo"), String.Empty) },
                { "bar", new SlnItem(Path.GetFullPath("bar"), "build") }
            };

            IBuildEngine buildEngine = new MockBuildEngine();

            SlnGen slnGen = new SlnGen
            {
                BuildEngine = buildEngine,
                SolutionItems = solutionItems.Select(i => new MockTaskItem(i.Key)
                {
                    { "FullPath", i.Value.FullPath },
                    { "TargetFolder", i.Value.TargetFolder }
                }).ToArray<ITaskItem>()
            };

            slnGen.GetSolutionItems(path => true).Select(i => i.TargetFolder).ShouldAllBe(f => !String.IsNullOrEmpty(f));
        }

        private static void ValidateParseCustomProjectTypeGuids(string fileExtension, string projectTypeGuid, string expectedFileExtension, Guid expectedProjectTypeGuid)
        {
            ITaskItem[] customProjectTypeGuids =
            {
                new MockTaskItem(fileExtension)
                {
                    { SlnGen.CustomProjectTypeGuidMetadataName, projectTypeGuid }
                }
            };

            ValidateParseCustomProjectTypeGuids(customProjectTypeGuids, expectedFileExtension, expectedProjectTypeGuid);
        }

        private static void ValidateParseCustomProjectTypeGuids(ITaskItem[] customProjectTypeGuids, string expectedFileExtension, Guid expectedProjectTypeGuid)
        {
            SlnGen slnGen = new SlnGen
            {
                CustomProjectTypeGuids = customProjectTypeGuids
            };

            Dictionary<string, Guid> actualProjectTypeGuids = slnGen.ParseCustomProjectTypeGuids();

            KeyValuePair<string, Guid> actualProjectTypeGuid = actualProjectTypeGuids.ShouldHaveSingleItem();

            actualProjectTypeGuid.Key.ShouldBe(expectedFileExtension);
            actualProjectTypeGuid.Value.ShouldBe(expectedProjectTypeGuid);
        }
    }
}