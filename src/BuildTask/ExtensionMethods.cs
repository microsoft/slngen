using System;
using System.Reflection;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;

namespace SlnGen.Build.Tasks
{
    using Microsoft.Build.Evaluation;

    internal static class ExtensionMethods
    {
        private static readonly Lazy<Assembly> BuildManagerAssemblyLazy = new Lazy<Assembly>(() => typeof(BuildManager).Assembly);

        private static readonly Lazy<Type> BuildRequestEntryTypeLazy = new Lazy<Type>(() => BuildManagerAssemblyLazy.Value.GetType("Microsoft.Build.BackEnd.BuildRequestEntry", throwOnError: false));

        private static readonly Lazy<Type> BuildRequestConfigurationTypeLazy = new Lazy<Type>(() => BuildManagerAssemblyLazy.Value.GetType("Microsoft.Build.BackEnd.BuildRequestConfiguration", throwOnError: false));

        private static readonly Lazy<PropertyInfo> BuildRequestEntryRequestConfigurationPropertyInfo = new Lazy<PropertyInfo>(() => BuildRequestEntryTypeLazy.Value.GetProperty("RequestConfiguration"));

        private static readonly Lazy<PropertyInfo> BuildRequestConfigurationProjectPropertyInfo = new Lazy<PropertyInfo>(() => BuildRequestConfigurationTypeLazy.Value.GetProperty("Project"));


        public static ProjectInstance GetProjectInstance(this IBuildEngine buildEngine)
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
                        return BuildRequestConfigurationProjectPropertyInfo.Value.GetValue(requestConfiguration) as ProjectInstance; ;
                    }
                }
            }

            return null;
        }

        public static string GetPropertyValueOrDefault(this Project project, string name, string defaultValue = null)
        {
            var value = project.GetPropertyValue(name);
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }
    }
}