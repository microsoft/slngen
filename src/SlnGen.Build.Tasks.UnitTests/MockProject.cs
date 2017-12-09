using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.IO;

namespace SlnGen.Build.Tasks.UnitTests
{
    public static class MockProject
    {
        public const string MockToolsVersion = "1.0";

        private static readonly Lazy<ProjectCollection> ProjectCollectionLazy = new Lazy<ProjectCollection>(CreateProjectCollection, isThreadSafe: true);

        public static ProjectCollection ProjectCollection => ProjectCollectionLazy.Value;

        public static Project Create(string fullPath, IDictionary<string, string> globalProperties)
        {
            ProjectRootElement rootElement = ProjectRootElement.Create(fullPath);

            Project project = new Project(globalProperties: globalProperties, toolsVersion: MockToolsVersion, projectCollection: ProjectCollectionLazy.Value, xml: rootElement);

            return project;
        }

        private static ProjectCollection CreateProjectCollection()
        {
            ProjectCollection projectCollection = new ProjectCollection(globalProperties: null, loggers: null, toolsetDefinitionLocations: ToolsetDefinitionLocations.None);

            projectCollection.AddToolset(new Toolset(MockToolsVersion, Path.GetTempPath(), projectCollection, msbuildOverrideTasksPath: null));

            return projectCollection;
        }
    }
}