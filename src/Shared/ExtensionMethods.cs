// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents extension methods used by this assembly.
    /// </summary>
    internal static class ExtensionMethods
    {
        /// <summary>
        /// Represents an array of <see cref="char" /> containing an equal sign.
        /// </summary>
        public static readonly char[] EqualsSign = { '=' };

        /// <summary>
        /// Represents an array of <see cref="char" /> containing an semicolon.
        /// </summary>
        public static readonly char[] Semicolon = { ';' };

        /// <summary>
        /// Characters to use when splitting argument values.
        /// </summary>
        private static readonly char[] ArgumentSplitChars = { ';', ',' };

        /// <summary>
        /// Returns a value indicating whether a specified substring occurs within this string.
        /// </summary>
        /// <param name="str">The <see cref="string" /> to check the occurrence of the substring in.</param>
        /// <param name="value">The string to seek.</param>
        /// <param name="comparisonType">One of the enumeration values that specifies the rules for the search.</param>
        /// <returns>true if the value parameter occurs within this string, or if value is the empty string (""); otherwise, false.</returns>
        public static bool Contains(this string str, string value, StringComparison comparisonType)
        {
            return value.IndexOf(str, comparisonType) >= 0;
        }

        /// <summary>
        /// Gets the value of the given conditioned property in this project.
        /// </summary>
        /// <param name="project">The <see cref="Project"/> to get the property value from.</param>
        /// <param name="name">The name of the property to get the value of.</param>
        /// <param name="defaultValue">A default values comma separated to return in the case when the property has no value.</param>
        /// <returns>The values of the property if one exists, otherwise the default value specified.</returns>
        public static IEnumerable<string> GetConditionedPropertyValuesOrDefault(this Project project, string name, string defaultValue)
        {
            if (!project.ConditionedProperties.ContainsKey(name))
            {
                return defaultValue.Split(',');
            }

            return project.ConditionedProperties[name];
        }

        /// <summary>
        /// Gets the value of the property value in addition to the conditioned property in this project.
        /// </summary>
        /// <param name="project">The <see cref="Project"/> to get the property value from.</param>
        /// <param name="name">The name of the property to get the value of.</param>
        /// <param name="defaultValue">A default values comma separated to return in the case when the property has no value.</param>
        /// <returns>The values of the property if one exists, otherwise the default value specified.</returns>
        public static IEnumerable<string> GetPossiblePropertyValuesOrDefault(this Project project, string name, string defaultValue)
        {
            HashSet<string> values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string conditionPropertyValue in project.GetConditionedPropertyValuesOrDefault(name, defaultValue))
            {
                values.Add(conditionPropertyValue);
            }

            if (!values.Any())
            {
                string propertyValue = project.GetPropertyValue(name);

                // add the actual properties first
                if (!string.IsNullOrEmpty(propertyValue))
                {
                    values.Add(propertyValue);
                }
            }

            return values.Any() ? values : (defaultValue?.Split(',') ?? Enumerable.Empty<string>());
        }

        /// <summary>
        /// Gets the value of the given property in this project.
        /// </summary>
        /// <param name="project">The <see cref="Project"/> to get the property value from.</param>
        /// <param name="name">The name of the property to get the value of.</param>
        /// <param name="defaultValue">A default value to return in the case when the property has no value.</param>
        /// <returns>The value of the property if one exists, otherwise the default value specified.</returns>
        public static string GetPropertyValueOrDefault(this Project project, string name, string defaultValue)
        {
            string value = project.GetPropertyValue(name);

            // MSBuild always returns String.Empty if the property has no value
            return value == string.Empty ? defaultValue : value;
        }

        /// <inheritdoc cref="string.IsNullOrWhiteSpace" />
        public static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Determines if a project's property value is considered true.
        /// </summary>
        /// <param name="project">The <see cref="Project" /> to get the property of.</param>
        /// <param name="name">The name of the property.</param>
        /// <returns>true if the property is considered true, otherwise false.</returns>
        public static bool IsPropertyValueTrue(this Project project, string name)
        {
            return project.GetPropertyValue(name).Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Splits the current string as a semicolon delimited list of equals sign separated key/value pairs.
        /// </summary>
        /// <param name="value">The value to split.</param>
        /// <returns>The current string split as a semicolon delimited list of equals sign separated key/value pairs.</returns>
        public static IEnumerable<KeyValuePair<string, string>> SplitProperties(this string value)
        {
            if (value.IsNullOrWhiteSpace())
            {
                return Enumerable.Empty<KeyValuePair<string, string>>();
            }

            return SplitSemicolonDelimitedList(value)
                .Select(i => i.Split(EqualsSign, 2, StringSplitOptions.RemoveEmptyEntries)) // Split by '='
                .Where(i => i.Length == 2 && !string.IsNullOrWhiteSpace(i[0]) && !string.IsNullOrWhiteSpace(i[1]))
                .Select(i => new KeyValuePair<string, string>(i.First().Trim(), i.Last().Trim()));
        }

        /// <summary>
        /// Parses the current string as a list of semicolon delimited items.
        /// </summary>
        /// <param name="value">The value to split.</param>
        /// <returns>An <see cref="IEnumerable{String}" /> containing the current string as a list of semicolon delimited items.</returns>
        public static IEnumerable<string> SplitSemicolonDelimitedList(this string value)
        {
            if (value.IsNullOrWhiteSpace())
            {
                return Enumerable.Empty<string>();
            }

            return value.Split(Semicolon, StringSplitOptions.RemoveEmptyEntries)
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Select(i => i.Trim());
        }

        /// <summary>
        /// Splits the specified values.
        /// </summary>
        /// <param name="source">An <see cref="IEnumerable{T}" /> containing comma or semicolon delimited values.</param>
        /// <returns>An <see cref="IReadOnlyCollection{String}" /> containing the split values.</returns>
        public static IReadOnlyCollection<string> SplitValues(this IEnumerable<string> source)
        {
            if (source == null)
            {
                return Array.Empty<string>();
            }

            HashSet<string> values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string item in source)
            {
                foreach (string value in item.Split(ArgumentSplitChars, StringSplitOptions.RemoveEmptyEntries)
                    .Where(i => !string.IsNullOrWhiteSpace(i))
                    .Select(i => i.Trim()))
                {
                    values.Add(value.Trim());
                }
            }

            return values;
        }

        /// <summary>
        /// Returns the absolute path for the specified path string in the correct case according to the file system.
        /// </summary>
        /// <param name="path">The string.</param>
        /// <returns>Full path in correct case.</returns>
        public static string ToFullPathInCorrectCase(this string path)
        {
            string fullPath = Path.GetFullPath(path);

            if (!File.Exists(fullPath))
            {
                return path;
            }

#if NETFRAMEWORK
            using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            StringBuilder stringBuilder = new StringBuilder(NativeMethods.GetFinalPathNameByHandle(stream.SafeFileHandle, null, 0, 0));

            NativeMethods.GetFinalPathNameByHandle(stream.SafeFileHandle, stringBuilder, stringBuilder.Capacity, 0);

            return stringBuilder.ToString(4, stringBuilder.Capacity - 5);
#else
            return fullPath;
#endif
        }

        /// <summary>
        /// Gets the current path as relative to the specified path.
        /// </summary>
        /// <param name="path">The current path to make relative.</param>
        /// <param name="relativeTo">A path to make relative to.</param>
        /// <returns>The current path as a relative path.</returns>
        public static string ToRelativePath(this string path, string relativeTo)
        {
            FileInfo fullPath = new FileInfo(Path.GetFullPath(path));

            FileInfo relativeFullPath = new FileInfo(Path.GetFullPath(relativeTo));

            if (fullPath.Directory == null || relativeFullPath.Directory == null || !string.Equals(fullPath.Directory.Root.FullName, relativeFullPath.Directory.Root.FullName))
            {
                return fullPath.FullName;
            }

            Uri fullPathUri = new Uri(fullPath.FullName, UriKind.Absolute);
            Uri relativePathUri = new Uri(relativeFullPath.FullName, UriKind.Absolute);

            return Uri.UnescapeDataString(relativePathUri.MakeRelativeUri(fullPathUri).ToString()).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Gets the current project platform value as a solution platform.
        /// </summary>
        /// <param name="platform">The current project platform.</param>
        /// <returns>The current project platform as a solution platform.</returns>
        public static string ToSolutionPlatform(this string platform)
        {
            if (platform.Equals("AnyCPU", StringComparison.OrdinalIgnoreCase))
            {
                return "Any CPU";
            }

            return platform;
        }

        /// <summary>
        /// Gets the current Guid as a string for use in a Visual Studio solution file.
        /// </summary>
        /// <param name="guid">The unique identifier.</param>
        /// <returns>
        /// The current GUID in as a string with braces and in upper case.
        /// </returns>
        public static string ToSolutionString(this Guid guid)
        {
            return guid.ToString("B").ToUpperInvariant();
        }
    }
}