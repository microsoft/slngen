namespace SlnGen.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.Build.Evaluation;

    /// <summary>
    /// Solution class.
    /// </summary>
    public class Solution
    {
        /// <summary>
        /// The solution header
        /// </summary>
        private const string Header = "Microsoft Visual Studio Solution File, Format Version 12.00";

        /// <summary>
        /// The configurations
        /// </summary>
        private readonly string[] configurations = { "Debug", "Release" };

        /// <summary>
        /// The platforms
        /// </summary>
        private readonly string[] platforms = { "Any CPU" };

        /// <summary>
        /// Initializes a new instance of the <see cref="Solution"/> class.
        /// </summary>
        /// <param name="configurations">The configurations.</param>
        /// <param name="platforms">The platforms.</param>
        public Solution(string[] configurations, string[] platforms)
            : this()
        {
            this.configurations = configurations;
            this.platforms = platforms;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Solution"/> class.
        /// </summary>
        public Solution()
        {
            this.Guid = System.Guid.NewGuid().ToString().ToUpperInvariant();
            this.Projects = new List<Project>();
        }

        /// <summary>
        /// Gets or sets the projects.
        /// </summary>
        public List<Project> Projects { get; set; }

        /// <summary>
        /// Gets the unique identifier.
        /// </summary>
        public string Guid { get; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            var builder = new StringBuilder(Header);
            builder.AppendLine();
            foreach (var project in this.Projects)
            {
                builder.AppendLine(project.ToSolutionSection());
            }

            builder.AppendLine("Global");
            builder.AppendLine(this.BuildSolutionConfigurationPlatforms());
            builder.AppendLine(this.BuildProjectConfigurationPlatforms());
            builder.AppendLine("EndGlobal");

            return builder.ToString();
        }

        /// <summary>
        /// Creates a global section
        /// </summary>
        /// <returns>global section</returns>
        private static string CreateGlobalSection(string sectionName, string sectionContent)
        {
            return $@"	GlobalSection({sectionName}) = preSolution{Environment.NewLine}{sectionContent}	EndGlobalSection";
        }

        private string BuildSolutionConfigurationPlatforms()
        {
            var builder = new StringBuilder();
            foreach (var configuration in this.configurations)
            {
                foreach (var platform in this.platforms)
                {
                    builder.AppendLine($@"		{configuration}|{platform} = {configuration}|{platform}");
                }
            }

            return CreateGlobalSection("SolutionConfigurationPlatforms", builder.ToString());
        }

        private string BuildProjectConfigurationPlatforms()
        {
            var builder = new StringBuilder();
            foreach (var project in this.Projects)
            {
                foreach (var configuration in this.configurations)
                {
                    foreach (var platform in this.platforms)
                    {
                        var guid = project.GetPropertyValue("ProjectGuid");
                        builder.AppendLine($@"		{guid}.{configuration}|{platform}.ActiveCfg = {configuration}|{platform}");
                        builder.AppendLine($@"		{guid}.{configuration}|{platform}.Build.0 = {configuration}|{platform}");
                    }
                }
            }

            return CreateGlobalSection("ProjectConfigurationPlatforms", builder.ToString());
        }
    }
}
