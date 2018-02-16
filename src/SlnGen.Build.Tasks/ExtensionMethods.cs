using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using SlnGen.Build.Tasks.Internal;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SlnGen.Build.Tasks
{
    using System.Collections.Generic;

    internal static class ExtensionMethods
    {
        #region Types used for getting to internal properties of the MSBuild API

        /// <summary>
        /// Stores the <see cref="Assembly"/> containing the type <see cref="BuildManager"/>.
        /// </summary>
        private static readonly Lazy<Assembly> BuildManagerAssemblyLazy = new Lazy<Assembly>(() => typeof(BuildManager).Assembly);

        /// <summary>
        /// Stores the <see cref="PropertyInfo"/> for the <see cref="Microsoft.Build.BackEnd.BuildRequestConfiguration.Project"/> property.
        /// </summary>
        private static readonly Lazy<PropertyInfo> BuildRequestConfigurationProjectPropertyInfo = new Lazy<PropertyInfo>(() => BuildRequestConfigurationTypeLazy.Value.GetProperty("Project"));

        /// <summary>
        /// Stores the <see cref="Type"/> of <see cref="Microsoft.Build.BackEnd.BuildRequestConfiguration"/>.
        /// </summary>
        private static readonly Lazy<Type> BuildRequestConfigurationTypeLazy = new Lazy<Type>(() => BuildManagerAssemblyLazy.Value.GetType("Microsoft.Build.BackEnd.BuildRequestConfiguration", throwOnError: false));

        /// <summary>
        /// Stores the <see cref="PropertyInfo"/> for the <see cref="Microsoft.Build.BackEnd.BuildRequestEntry.RequestConfiguration"/> property.
        /// </summary>
        private static readonly Lazy<PropertyInfo> BuildRequestEntryRequestConfigurationPropertyInfo = new Lazy<PropertyInfo>(() => BuildRequestEntryTypeLazy.Value.GetProperty("RequestConfiguration"));

        /// <summary>
        /// Stores the <see cref="Type"/> of <see cref="Microsoft.Build.BackEnd.BuildRequestEntry"/>.
        /// </summary>
        private static readonly Lazy<Type> BuildRequestEntryTypeLazy = new Lazy<Type>(() => BuildManagerAssemblyLazy.Value.GetType("Microsoft.Build.BackEnd.BuildRequestEntry", throwOnError: false));

        #endregion Types used for getting to internal properties of the MSBuild API

        /// <summary>
        /// Converts the specified path to its long form.
        /// </summary>
        /// <returns>The specified path in its long form and correct case according to the file system.</returns>
        public static string GetLongPathName(this DirectoryInfo directoryInfo)
        {
            if (!directoryInfo.Exists)
            {
                throw new DirectoryNotFoundException($"Could not find part of the path \"{directoryInfo.FullName}\"");
            }

            StringBuilder stringBuilder = new StringBuilder(directoryInfo.FullName.Length + 1);

            int result = NativeMethods.GetLongPathName(directoryInfo.FullName, stringBuilder, stringBuilder.Capacity);

            stringBuilder[0] = char.ToUpperInvariant(stringBuilder[0]);

            return stringBuilder.ToString(0, result);
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
            return value == String.Empty ? defaultValue : value;
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
        /// Returns the absolute path for the specified path string in the correct case according to the file system.
        /// </summary>
        public static string ToFullPathInCorrectCase(this string str)
        {
            string fullPath = Path.GetFullPath(str);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Could not find part of the path \"{fullPath}\"");
            }

            string filename = Path.GetFileName(fullPath);

            string directory = Directory.GetParent(fullPath).GetLongPathName();

            return Directory.EnumerateFiles(directory, filename).First();
        }

        /// <summary>
        /// Gets the current Guid as a string for use in a Visual Studio solution file.
        /// </summary>
        /// <returns>The current GUID in as a string with braces and in upper case.</returns>
        public static string ToSolutionString(this Guid guid)
        {
            return guid.ToString("B").ToUpperInvariant();
        }

        /// <summary>
        /// Attempts to get the current <see cref="ProjectInstance"/> of the executing task via reflection.
        /// </summary>
        /// <param name="buildEngine">A <see cref="IBuildEngine"/> for the currently executing task.</param>
        /// <param name="projectInstance">Receives a <see cref="ProjectInstance"/> object if one could be determined.</param>
        /// <returns><code>true</code> if the current <see cref="ProjectInstance"/> could be determined, otherwise <code>false</code>.</returns>
        public static bool TryGetProjectInstance(this IBuildEngine buildEngine, out ProjectInstance projectInstance)
        {
            projectInstance = null;

            try
            {
                FieldInfo requestEntryFieldInfo = buildEngine.GetType().GetField("_requestEntry", BindingFlags.Instance | BindingFlags.NonPublic);

                if (requestEntryFieldInfo != null && BuildRequestEntryTypeLazy.Value != null && BuildRequestConfigurationTypeLazy.Value != null)
                {
                    object requestEntry = requestEntryFieldInfo.GetValue(buildEngine);

                    if (requestEntry != null && BuildRequestEntryRequestConfigurationPropertyInfo.Value != null)
                    {
                        object requestConfiguration = BuildRequestEntryRequestConfigurationPropertyInfo.Value.GetValue(requestEntry);

                        if (requestConfiguration != null && BuildRequestConfigurationProjectPropertyInfo.Value != null)
                        {
                            projectInstance = BuildRequestConfigurationProjectPropertyInfo.Value.GetValue(requestConfiguration) as ProjectInstance;

                            return true;
                        }
                    }
                }
            }
            catch
            {
                // Ignored because we never want this method to throw since its using reflection to access internal members that could go away with any future release of MSBuild
            }

            return false;
        }
    }
}