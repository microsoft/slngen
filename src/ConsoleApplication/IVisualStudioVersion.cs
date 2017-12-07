namespace SlnGen
{
    internal interface IVisualStudioVersion
    {
        string ToolsPath { get; }
        string FileFormatVersion { get; }
        string Version { get; }
        string ImportsRegistryKey { get; }
    }
}