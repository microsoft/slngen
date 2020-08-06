// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Shouldly;
using System.IO;
using System.Text;
using Xunit;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    public class MSBuildLocatorTests
    {
        [Fact]
        public void FactMethodName()
        {
            const string output = @".NET Core SDK (reflecting any global.json):
 Version: 3.1.302
 Commit: 41faccf259

Runtime Environment:
 OS Name: Windows
 OS Version: 10.0.19041
 OS Platform: Windows
 RID: win10-x64
 Base Path: C:\Program Files\dotnet\sdk\3.1.302\

Host (useful for support):
 Version: 3.1.6
 Commit: 3acd9b0cd1

.NET Core SDKs installed:
 2.1.202 [C:\Program Files\dotnet\sdk]
 2.1.503 [C:\Program Files\dotnet\sdk]
 2.1.509 [C:\Program Files\dotnet\sdk]
 2.1.511 [C:\Program Files\dotnet\sdk]
 2.1.512 [C:\Program Files\dotnet\sdk]
 2.1.513 [C:\Program Files\dotnet\sdk]
 2.1.514 [C:\Program Files\dotnet\sdk]
 2.1.515 [C:\Program Files\dotnet\sdk]
 2.1.516 [C:\Program Files\dotnet\sdk]
 2.1.801 [C:\Program Files\dotnet\sdk]
 2.2.401 [C:\Program Files\dotnet\sdk]
 3.0.103 [C:\Program Files\dotnet\sdk]
 3.1.302 [C:\Program Files\dotnet\sdk]

.NET Core runtimes installed:
 Microsoft.AspNetCore.All 2.1.7 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.All]
 Microsoft.AspNetCore.All 2.1.12 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.All]
 Microsoft.AspNetCore.All 2.1.13 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.All]
 Microsoft.AspNetCore.All 2.1.15 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.All]
 Microsoft.AspNetCore.All 2.1.16 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.All]
 Microsoft.AspNetCore.All 2.1.17 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.All]
 Microsoft.AspNetCore.All 2.1.18 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.All]
 Microsoft.AspNetCore.All 2.1.19 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.All]
 Microsoft.AspNetCore.All 2.1.20 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.All]
 Microsoft.AspNetCore.All 2.2.6 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.All]
 Microsoft.AspNetCore.App 2.1.7 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
 Microsoft.AspNetCore.App 2.1.12 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
 Microsoft.AspNetCore.App 2.1.13 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
 Microsoft.AspNetCore.App 2.1.15 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
 Microsoft.AspNetCore.App 2.1.16 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
 Microsoft.AspNetCore.App 2.1.17 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
 Microsoft.AspNetCore.App 2.1.18 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
 Microsoft.AspNetCore.App 2.1.19 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
 Microsoft.AspNetCore.App 2.1.20 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
 Microsoft.AspNetCore.App 2.2.6 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
 Microsoft.AspNetCore.App 3.0.3 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
 Microsoft.AspNetCore.App 3.1.6 [C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App]
 Microsoft.NETCore.App 2.0.9 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
 Microsoft.NETCore.App 2.1.7 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
 Microsoft.NETCore.App 2.1.12 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
 Microsoft.NETCore.App 2.1.13 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
 Microsoft.NETCore.App 2.1.15 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
 Microsoft.NETCore.App 2.1.16 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
 Microsoft.NETCore.App 2.1.17 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
 Microsoft.NETCore.App 2.1.18 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
 Microsoft.NETCore.App 2.1.19 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
 Microsoft.NETCore.App 2.1.20 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
 Microsoft.NETCore.App 2.2.6 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
 Microsoft.NETCore.App 3.0.3 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
 Microsoft.NETCore.App 3.1.6 [C:\Program Files\dotnet\shared\Microsoft.NETCore.App]
 Microsoft.WindowsDesktop.App 3.0.3 [C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App]
 Microsoft.WindowsDesktop.App 3.1.6 [C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App]

To install additional .NET Core runtimes or SDKs:
 https://aka.ms/dotnet-download
";
            string basePath;

            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(output)))
            {
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    MSBuildLocator.TryGetDotNetCoreBasePath(reader, out basePath).ShouldBeTrue();
                }
            }

            basePath.ShouldBe(@"C:\Program Files\dotnet\sdk\3.1.302\");
        }
    }
}
