// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System;

namespace SlnGen.Common
{
    internal class MSBuildFeatureFlags : IDisposable
    {
        /// <summary>
        /// Represents a regular expression that matches any file spec that contains a wildcard * or ? and does not end in "proj".
        /// </summary>
        private const string SkipWildcardRegularExpression = @"[*?]+.*(?<!proj)$";

        private string _cacheFileEnumerations;
        private string _loadAllFilesAsReadonly;
        private string _msbuildExePath;
        private string _skipEagerWildcardEvaluations;
        private string _useSimpleProjectRootElementCacheConcurrency;

        /// <summary>
        /// Gets or sets the full path to MSBuild that should be used to evaluate projects.
        /// </summary>
        /// <remarks>
        /// MSBuild is not installed globally anymore as of version 15.0.  Processes doing evaluations must set this environment variable for the toolsets
        /// to be found by MSBuild (stuff like $(MSBuildExtensionsPath).
        /// More info here: https://github.com/microsoft/msbuild/blob/master/src/Shared/BuildEnvironmentHelper.cs#L125
        /// </remarks>
        public string MSBUILD_EXE_PATH
        {
            get => Environment.GetEnvironmentVariable(nameof(MSBUILD_EXE_PATH));
            set
            {
                _msbuildExePath = Environment.GetEnvironmentVariable(nameof(MSBUILD_EXE_PATH));

                Environment.SetEnvironmentVariable(nameof(MSBUILD_EXE_PATH), value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating if wildcard expansions for the entire process should be cached.
        /// </summary>
        /// <remarks>
        /// More info here: https://github.com/microsoft/msbuild/blob/master/src/Shared/Traits.cs#L55
        /// </remarks>
        public bool MSBuildCacheFileEnumerations
        {
            get => string.Equals(Environment.GetEnvironmentVariable(nameof(MSBuildCacheFileEnumerations)), "1");
            set
            {
                _cacheFileEnumerations = Environment.GetEnvironmentVariable(nameof(MSBuildCacheFileEnumerations));

                Environment.SetEnvironmentVariable(nameof(MSBuildCacheFileEnumerations), value ? "1" : null);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating if all projects should be treated as read-only which enables an optimized way of
        /// reading them.
        /// </summary>
        /// <remarks>
        /// More info here: https://github.com/microsoft/msbuild/blob/master/src/Build/ElementLocation/XmlDocumentWithLocation.cs#L392
        /// </remarks>
        public bool MSBuildLoadAllFilesAsReadonly
        {
            get => string.Equals(Environment.GetEnvironmentVariable(nameof(MSBuildLoadAllFilesAsReadonly)), "1");
            set
            {
                _loadAllFilesAsReadonly = Environment.GetEnvironmentVariable(nameof(MSBuildLoadAllFilesAsReadonly));

                Environment.SetEnvironmentVariable(nameof(MSBuildLoadAllFilesAsReadonly), value ? "1" : null);
            }
        }

        /// <summary>
        /// Gets or sets a set of regular expressions that MSBuild uses in order to skip the evaluation of items. If any expression
        /// matches the item, its Include is left as a literal string rather than expanding it.  .NET Core SDK ships with default
        /// item includes like **\*, **\*.cs, and **\*.resx.
        ///
        /// Users can also unknowingly introduce a run away wildcard like:
        ///   $(MyProperty)\**
        ///
        /// If $(MyProperty) is not set this would evaluate to "\**" which would cause MSBuild to enumerate the entire disk.
        ///
        /// The only wildcard NuGet needs to respect is something like **\*.*proj which allows users to specify that they want to
        /// restore every MSBuild project in their repo.
        /// </summary>
        /// <remarks>
        /// More info here: https://github.com/microsoft/msbuild/blob/master/src/Build/Utilities/EngineFileUtilities.cs#L221
        /// </remarks>
        public bool MSBuildSkipEagerWildCardEvaluationRegexes
        {
            get => !string.Equals(Environment.GetEnvironmentVariable("MSBuildSkipEagerWildCardEvaluationRegexes"), null);
            set
            {
                _skipEagerWildcardEvaluations = Environment.GetEnvironmentVariable(nameof(MSBuildSkipEagerWildCardEvaluationRegexes));

                Environment.SetEnvironmentVariable(nameof(MSBuildSkipEagerWildCardEvaluationRegexes), SkipWildcardRegularExpression);
            }
        }

        public bool MSBuildUseSimpleProjectRootElementCacheConcurrency
        {
            get => !string.Equals(Environment.GetEnvironmentVariable(nameof(MSBuildUseSimpleProjectRootElementCacheConcurrency)), "1");
            set
            {
                _useSimpleProjectRootElementCacheConcurrency = Environment.GetEnvironmentVariable(nameof(MSBuildUseSimpleProjectRootElementCacheConcurrency));

                Environment.SetEnvironmentVariable(nameof(MSBuildUseSimpleProjectRootElementCacheConcurrency), "1");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Environment.SetEnvironmentVariable(nameof(MSBuildSkipEagerWildCardEvaluationRegexes), _skipEagerWildcardEvaluations);
            Environment.SetEnvironmentVariable(nameof(MSBUILD_EXE_PATH), _msbuildExePath);
            Environment.SetEnvironmentVariable(nameof(MSBuildLoadAllFilesAsReadonly), _loadAllFilesAsReadonly);
            Environment.SetEnvironmentVariable(nameof(MSBuildUseSimpleProjectRootElementCacheConcurrency), _useSimpleProjectRootElementCacheConcurrency);
            Environment.SetEnvironmentVariable(nameof(MSBuildCacheFileEnumerations), _cacheFileEnumerations);
        }
    }
}