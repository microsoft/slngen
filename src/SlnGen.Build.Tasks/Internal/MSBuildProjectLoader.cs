// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SlnGen.Build.Tasks.Internal
{
    /// <summary>
    /// A class for loading MSBuild projects and their project references.
    /// </summary>
    internal sealed class MSBuildProjectLoader
    {
        /// <summary>
        /// The name of the <ProjectReference /> item in MSBuild projects.
        /// </summary>
        private const string ProjectReferenceItemName = "ProjectReference";

        /// <summary>
        /// The name of the environment variable that configures MSBuild to ignore eager wildcard evaluations (like \**)
        /// </summary>
        private const string MSBuildSkipEagerWildcardEvaluationsEnvironmentVariableName = "MSBUILDSKIPEAGERWILDCARDEVALUATIONREGEXES";

        private readonly IBuildEngine _buildEngine;

        /// <summary>
        /// Stores the global properties to use when loading projects.
        /// </summary>
        private readonly IDictionary<string, string> _globalProperties;

        /// <summary>
        /// Stores the list of paths to the projects that are loaded.
        /// </summary>
        private readonly HashSet<string> _loadedProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Stores the <see cref="ProjectLoadSettings"/> to use when loading projects.
        /// </summary>
        private readonly ProjectLoadSettings _projectLoadSettings;

        /// <summary>
        /// Stores the ToolsVersion to use when loading projects.
        /// </summary>
        private readonly string _toolsVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="MSBuildProjectLoader"/> class.
        /// </summary>
        /// <param name="globalProperties">Specifies the global properties to use when loading projects.</param>
        /// <param name="toolsVersion">Specifies the ToolsVersion to use when loading projects.</param>
        /// <param name="buildEngine">An <see cref="IBuildEngine"/> object to use for logging.</param>
        /// <param name="projectLoadSettings">Specifies the <see cref="ProjectLoadSettings"/> to use when loading projects.</param>
        public MSBuildProjectLoader(IDictionary<string, string> globalProperties, string toolsVersion, IBuildEngine buildEngine, ProjectLoadSettings projectLoadSettings = ProjectLoadSettings.Default)
        {
            _globalProperties = globalProperties;
            _toolsVersion = toolsVersion;
            _projectLoadSettings = projectLoadSettings;
            _buildEngine = buildEngine ?? throw new ArgumentNullException(nameof(buildEngine));
        }

        /// <summary>
        /// Gets or sets a value indicating whether statistics should be collected.
        /// </summary>
        public bool CollectStats { get; set; } = false;

        /// <summary>
        /// Gets or sets a <see cref="Func{Project, Boolean}"/> that determines if a project is a traversal project.
        /// </summary>
        public Func<Project, bool> IsTraveralProject { get; set; } = project => String.Equals("true", project.GetPropertyValue("IsTraversal"));

        public MSBuildProjectLoaderStatistics Statistics { get; } = new MSBuildProjectLoaderStatistics();

        /// <summary>
        /// Gets or sets the item names that specify project references in a traversal project.  The default value is "ProjectFile".
        /// </summary>
        public string TraveralProjectFileItemName { get; set; } = "ProjectFile";

        /// <summary>
        /// Loads the specified projects and their references.
        /// </summary>
        /// <param name="projectPaths">An <see cref="IEnumerable{String}"/> containing paths to the projects to load.</param>
        /// <returns>A <see cref="ProjectCollection"/> object containing the loaded projects.</returns>
        public ProjectCollection LoadProjectsAndReferences(IEnumerable<string> projectPaths)
        {
            // Store the current value of the environment variable that disables eager wildcard evaluations
            string currentSkipEagerWildcardEvaluationsValue = Environment.GetEnvironmentVariable(MSBuildSkipEagerWildcardEvaluationsEnvironmentVariableName);

            try
            {
                // Indicate to MSBuild that any item that has two wildcards should be evaluated lazily
                Environment.SetEnvironmentVariable(MSBuildSkipEagerWildcardEvaluationsEnvironmentVariableName, @"\*{2}");

                // Create a ProjectCollection for this thread
                ProjectCollection projectCollection = new ProjectCollection(_globalProperties)
                {
                    DefaultToolsVersion = _toolsVersion,
                    DisableMarkDirty = true, // Not sure but hoping this improves load performance
                };

                Parallel.ForEach(projectPaths, projectPath => { LoadProject(projectPath, projectCollection, _projectLoadSettings); });

                return projectCollection;
            }
            finally
            {
                // Restore the environment variable value
                Environment.SetEnvironmentVariable(MSBuildSkipEagerWildcardEvaluationsEnvironmentVariableName, currentSkipEagerWildcardEvaluationsValue);
            }
        }

        /// <summary>
        /// Loads a single project if it hasn't already been loaded.
        /// </summary>
        /// <param name="projectPath">The path to the project.</param>
        /// <param name="projectCollection">A <see cref="ProjectCollection"/> to load the project into.</param>
        /// <param name="projectLoadSettings">Specifies the <see cref="ProjectLoadSettings"/> to use when loading projects.</param>
        private void LoadProject(string projectPath, ProjectCollection projectCollection, ProjectLoadSettings projectLoadSettings)
        {
            if (TryLoadProject(projectPath, projectCollection.DefaultToolsVersion, projectCollection, projectLoadSettings, out Project project))
            {
                LoadProjectReferences(project, _projectLoadSettings);
            }
        }

        /// <summary>
        /// Loads the project references of the specified project.
        /// </summary>
        /// <param name="project">The <see cref="Project"/> to load the references of.</param>
        /// <param name="projectLoadSettings">Specifies the <see cref="ProjectLoadSettings"/> to use when loading projects.</param>
        private void LoadProjectReferences(Project project, ProjectLoadSettings projectLoadSettings)
        {
            IEnumerable<ProjectItem> projects = project.GetItems(ProjectReferenceItemName);

            if (IsTraveralProject(project))
            {
                projects = projects.Concat(project.GetItems(TraveralProjectFileItemName));
            }

            Parallel.ForEach(projects, projectReferenceItem =>
            {
                string projectReferencePath = Path.IsPathRooted(projectReferenceItem.EvaluatedInclude) ? projectReferenceItem.EvaluatedInclude : Path.GetFullPath(Path.Combine(projectReferenceItem.Project.DirectoryPath, projectReferenceItem.EvaluatedInclude));

                LoadProject(projectReferencePath, projectReferenceItem.Project.ProjectCollection, projectLoadSettings);
            });
        }

        /// <summary>
        /// Attempts to load the specified project if it hasn't already been loaded.
        /// </summary>
        /// <param name="path">The path to the project to load.</param>
        /// <param name="toolsVersion">The ToolsVersion to use when loading the project.</param>
        /// <param name="projectCollection">The <see cref="ProjectCollection"/> to load the project into.</param>
        /// <param name="projectLoadSettings">Specifies the <see cref="ProjectLoadSettings"/> to use when loading projects.</param>
        /// <param name="project">Contains the loaded <see cref="Project"/> if one was loaded.</param>
        /// <returns><code>true</code> if the project was loaded, otherwise <code>false</code>.</returns>
        private bool TryLoadProject(string path, string toolsVersion, ProjectCollection projectCollection, ProjectLoadSettings projectLoadSettings, out Project project)
        {
            project = null;

            bool shouldLoadProject;

            string fullPath = path.ToFullPathInCorrectCase();

            lock (_loadedProjects)
            {
                shouldLoadProject = _loadedProjects.Add(fullPath);
            }

            if (!shouldLoadProject)
            {
                return false;
            }

            long now = DateTime.Now.Ticks;

            try
            {
                project = new Project(fullPath, null, toolsVersion, projectCollection, projectLoadSettings);
            }
            catch (InvalidProjectFileException e)
            {
                _buildEngine.LogErrorEvent(new BuildErrorEventArgs(
                    subcategory: null,
                    code: e.ErrorCode,
                    file: e.ProjectFile,
                    lineNumber: e.LineNumber,
                    columnNumber: e.ColumnNumber,
                    endLineNumber: e.EndLineNumber,
                    endColumnNumber: e.EndColumnNumber,
                    message: e.Message,
                    helpKeyword: e.HelpKeyword,
                    senderName: null));

                return false;
            }
            catch (Exception e)
            {
                _buildEngine.LogErrorEvent(new BuildErrorEventArgs(
                    subcategory: null,
                    code: null,
                    file: path,
                    lineNumber: 0,
                    columnNumber: 0,
                    endLineNumber: 0,
                    endColumnNumber: 0,
                    message: e.ToString(),
                    helpKeyword: null,
                    senderName: null));

                return false;
            }

            if (CollectStats)
            {
                Statistics.TryAddProjectLoadTime(path, TimeSpan.FromTicks(DateTime.Now.Ticks - now));
            }

            return true;
        }
    }
}