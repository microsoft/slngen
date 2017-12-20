using System.IO;

namespace SlnGen.Build.Tasks.Internal
{
    internal sealed class SlnFolder
    {
        public const string ProjectTypeGuid = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

        public SlnFolder(string path, string guid)
        {
            Name = Path.GetFileName(path);
            FullPath = path;
            Guid = guid;
        }

        public string FullPath { get; }

        public string Guid { get; }

        public string Name { get; }

        public string TypeGuid => ProjectTypeGuid;
    }
}