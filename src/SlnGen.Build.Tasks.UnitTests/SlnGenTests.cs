using Microsoft.Build.Framework;
using NUnit.Framework;
using Shouldly;
using System.Collections.Generic;

namespace SlnGen.Build.Tasks.UnitTests
{
    [TestFixture]
    public sealed class SlnGenTests : TestBase
    {
        [Test]
        public void ParseCustomProjectTypeGuidsDeduplicatesList()
        {
            const string expectedProjectTypeGuid = "{C139C737-2894-46A0-B1EB-DDD052FD8DCB}";

            ITaskItem[] customProjectTypeGuids =
            {
                new MockTaskItem(" .foo ")
                {
                    {SlnGen.CustomProjectTypeGuidMetadataName, "1AB09E1B-77F6-4982-B020-374DB9DF2BD2"}
                },
                new MockTaskItem(".foo")
                {
                    {SlnGen.CustomProjectTypeGuidMetadataName, expectedProjectTypeGuid}
                },
            };

            ValidateParseCustomProjectTypeGuids(customProjectTypeGuids, ".foo", expectedProjectTypeGuid);
        }

        [Test]
        public void ParseCustomProjectTypeGuidsFormatsFileExtensionAndGuid()
        {
            ValidateParseCustomProjectTypeGuids(
                " .FoO  ", "  9d9339782d2a4fb2b72d8746d88e73b7 ",
                ".foo", "{9D933978-2D2A-4FB2-B72D-8746D88E73B7}");
        }

        [Test]
        public void ParseCustomProjectTypeGuidsIgnoresNonFileExtensions()
        {
            const string expectedProjectTypeGuid = "{C139C737-2894-46A0-B1EB-DDD052FD8DCB}";

            ITaskItem[] customProjectTypeGuids =
            {
                new MockTaskItem("foo")
                {
                    {SlnGen.CustomProjectTypeGuidMetadataName, "9d933978-2d2a-4fb2-b72d-8746d88e73b7"}
                },
                new MockTaskItem(".foo")
                {
                    {SlnGen.CustomProjectTypeGuidMetadataName, expectedProjectTypeGuid}
                },
            };

            ValidateParseCustomProjectTypeGuids(customProjectTypeGuids, ".foo", expectedProjectTypeGuid);
        }

        private static void ValidateParseCustomProjectTypeGuids(string fileExtension, string projectTypeGuid, string expectedFileExtension, string expectedProjectTypeGuid)
        {
            ITaskItem[] customProjectTypeGuids =
            {
                new MockTaskItem(fileExtension)
                {
                    {SlnGen.CustomProjectTypeGuidMetadataName, projectTypeGuid}
                }
            };

            ValidateParseCustomProjectTypeGuids(customProjectTypeGuids, expectedFileExtension, expectedProjectTypeGuid);
        }

        private static void ValidateParseCustomProjectTypeGuids(ITaskItem[] customProjectTypeGuids, string expectedFileExtension, string expectedProjectTypeGuid)
        {
            SlnGen slnGen = new SlnGen
            {
                CustomProjectTypeGuids = customProjectTypeGuids
            };

            Dictionary<string, string> actualProjectTypeGuids = slnGen.ParseCustomProjectTypeGuids();

            KeyValuePair<string, string> actualProjectTypeGuid = actualProjectTypeGuids.ShouldHaveSingleItem();

            actualProjectTypeGuid.Key.ShouldBe(expectedFileExtension);
            actualProjectTypeGuid.Value.ShouldBe(expectedProjectTypeGuid);
        }
    }
}