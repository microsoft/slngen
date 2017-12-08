using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlnGen.Build.Tasks
{
    using System.IO;

    internal class FolderInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FolderInfo" /> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="guid">The unique identifier.</param>
        public FolderInfo(string path, string guid)
        {
            this.Name = Path.GetFileName(path);
            this.FullPath = path;
            this.Guid = guid;
        }

        public string TypeGuid => "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

        public string Name { get; }

        public string FullPath { get; }

        public string Guid { get; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return $@"Project(""{this.TypeGuid}"") = ""{this.Name}"", ""{this.FullPath}"", ""{this.Guid}""{Environment.NewLine}EndProject";
        }
    }
}
