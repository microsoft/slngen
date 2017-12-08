using System;
using System.IO;

namespace SlnGen.Build.Tasks
{
    internal class SolutionFolder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionFolder" /> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="guid">The unique identifier.</param>
        public SolutionFolder(string path, string guid)
        {
            Name = Path.GetFileName(path);
            FullPath = path;
            Guid = guid;
        }

        public string FullPath { get; }
        public string Guid { get; }
        public string Name { get; }
        public string TypeGuid => "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return $@"Project(""{TypeGuid}"") = ""{Name}"", ""{FullPath}"", ""{Guid}""{Environment.NewLine}EndProject";
        }
    }
}