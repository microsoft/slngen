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

        public static string ToSolutionSection(this Project project)
        {
            var guid = project.GetPropertyValue("ProjectGuid");
            var assemblyName = project.GetPropertyValue("AssemblyName");
            var path = project.FullPath;
            const string TypeGuid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
            return $@"Project(""{TypeGuid}"") = ""{assemblyName}"", ""{path}"", ""{guid}""{Environment.NewLine}EndProject";
        }
    }
}