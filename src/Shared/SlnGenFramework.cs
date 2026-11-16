// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System.Globalization;
using System.IO;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents the result of selecting the target framework of SlnGen to run for a .NET SDK.
    /// </summary>
    internal enum SlnGenFrameworkResult
    {
        /// <summary>
        /// A matching target framework was found.
        /// </summary>
        Success,

        /// <summary>
        /// The major version of the .NET SDK could not be determined.
        /// </summary>
        UnknownSdkVersion,

        /// <summary>
        /// The .NET SDK is older than the minimum version supported by SlnGen.
        /// </summary>
        UnsupportedSdkVersion,

        /// <summary>
        /// This version of SlnGen does not contain a build for the .NET SDK.
        /// </summary>
        SdkVersionNotAvailable,
    }

    /// <summary>
    /// Selects which target framework of SlnGen to run.
    /// </summary>
    internal static class SlnGenFramework
    {
        /// <summary>
        /// The minimum major version of the .NET SDK supported by SlnGen.
        /// </summary>
        public const int MinimumDotNetSdkMajorVersion = 10;

        /// <summary>
        /// Gets the target framework of SlnGen to run for the specified .NET SDK.
        /// </summary>
        /// <param name="dotNetSdkMajorVersion">The major version of the .NET SDK selected by global.json.</param>
        /// <param name="slnGenDirectory">The directory containing a subdirectory for each target framework of SlnGen.</param>
        /// <param name="framework">Receives the target framework of SlnGen to run if one is available.</param>
        /// <returns>A <see cref="SlnGenFrameworkResult" /> indicating whether a target framework was found.</returns>
        public static SlnGenFrameworkResult TryGetDotNetFramework(string dotNetSdkMajorVersion, string slnGenDirectory, out string framework)
        {
            framework = null;

            if (!int.TryParse(dotNetSdkMajorVersion, NumberStyles.None, CultureInfo.InvariantCulture, out int sdkMajorVersion))
            {
                return SlnGenFrameworkResult.UnknownSdkVersion;
            }

            if (sdkMajorVersion < MinimumDotNetSdkMajorVersion)
            {
                return SlnGenFrameworkResult.UnsupportedSdkVersion;
            }

            // SlnGen must run on the same major version of .NET as the SDK's MSBuild, so only use a build that exactly matches the SDK
            string candidate = $"net{sdkMajorVersion}.0";

            if (!Directory.Exists(Path.Combine(slnGenDirectory, candidate)))
            {
                return SlnGenFrameworkResult.SdkVersionNotAvailable;
            }

            framework = candidate;

            return SlnGenFrameworkResult.Success;
        }
    }
}
