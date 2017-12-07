using Microsoft.Win32;
using System;
using System.IO;

namespace SlnGen
{
    internal class VisualStudioGeneric : IVisualStudioVersion
    {
        private const string VisualStudioRegistryKey = @"SOFTWARE\Microsoft\VisualStudio\SxS\VS7";

        private readonly Lazy<string> _toolsPath;

        private readonly Version _version;

        public VisualStudioGeneric(Version version)
        {
            _version = version ?? throw new ArgumentNullException(nameof(version));

            _toolsPath = new Lazy<string>(FindVsPath, true);
        }

        public virtual string FileFormatVersion => "12.00";

        public virtual string ImportsRegistryKey => throw new NotImplementedException(
            $"Imports registry key is not known for VS {Version}");

        public string ToolsPath => _toolsPath.Value;

        public virtual string Version => $"{_version.Major}";

        private string FindVsPath()
        {
            /*
             * Starting from VS2017 they do not modify environment variables, so there is no %VS150COMNTOOLS%:
             * https://developercommunity.visualstudio.com/content/problem/13223/no-environmental-variable-vscomntools150.html
             * As I understand, this is done to allow you run multiple Editions of same version side-by-side.
             * Official documentation says that we should be using their installer COM components, but it sounds like an overkill:
             * https://blogs.msdn.microsoft.com/heaths/2016/09/15/changes-to-visual-studio-15-setup/
             * Samples: https://code.msdn.microsoft.com/Visual-Studio-Setup-0cedd331/sourcecode?fileId=159487&pathId=506200719
             * Instead we use a registry lookup as proposed here (unofficially, though):
             * https://developercommunity.visualstudio.com/content/problem/2813/cant-find-registry-entries-for-visual-studio-2017.html
            */

            // TODO: Improve logic
            string vsInstallDir = Environment.GetEnvironmentVariable("VSINSTALLDIR");

            if (!String.IsNullOrWhiteSpace(vsInstallDir) && Directory.Exists(vsInstallDir))
            {
                return Path.Combine(vsInstallDir, "Common7", "Tools");
            }

            using (RegistryKey keyHklm32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                using (RegistryKey vsKey = keyHklm32.OpenSubKey(VisualStudioRegistryKey, false))
                {
                    if (vsKey == null)
                    {
                        throw new InvalidOperationException($"Cannot open Visual Studio registry key '{VisualStudioRegistryKey}'");
                    }

                    string versionString = _version.ToString(2);
                    string rootPath = (string) vsKey.GetValue(versionString, null);

                    if (String.IsNullOrEmpty(rootPath))
                    {
                        throw new InvalidOperationException($"Installation path registry key for Visual Studio version '{versionString}' is not found");
                    }

                    return Path.Combine(rootPath, "Common7", "Tools");
                }
            }
        }
    }
}