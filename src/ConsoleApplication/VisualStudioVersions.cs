using System;

namespace SlnGen
{
    internal sealed class VisualStudio2010 : IVisualStudioVersion
    {
        public string FileFormatVersion => "11.00";

        public string ImportsRegistryKey => @"Microsoft\VisualStudio\10.0\MSBuild\SafeImports";

        public string ToolsPath => Environment.GetEnvironmentVariable("VS100COMNTOOLS");

        public string Version => "2010";
    }

    internal sealed class VisualStudio2012 : IVisualStudioVersion
    {
        public string FileFormatVersion => "12.00";
        public string ImportsRegistryKey => @"Microsoft\VisualStudio\11.0\MSBuild\SafeImports";
        public string ToolsPath => Environment.GetEnvironmentVariable("VS110COMNTOOLS");
        public string Version => "2012";
    }

    internal sealed class VisualStudio2013 : IVisualStudioVersion
    {
        public string FileFormatVersion => "12.00";

        public string ImportsRegistryKey => @"Microsoft\VisualStudio\12.0\MSBuild\SafeImports";

        public string ToolsPath => Environment.GetEnvironmentVariable("VS120COMNTOOLS");

        public string Version => "2013";
    }

    internal sealed class VisualStudio2015 : IVisualStudioVersion
    {
        public string FileFormatVersion => "12.00";

        public string ImportsRegistryKey => @"Microsoft\VisualStudio\14.0\MSBuild\SafeImports";

        public string ToolsPath => Environment.GetEnvironmentVariable("VS140COMNTOOLS");

        public string Version => "2015";
    }

    internal class VisualStudio2017 : VisualStudioGeneric
    {
        public VisualStudio2017()
            : base(new Version(15, 0))
        {
        }
    }
}