using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyTitle("SlnGen.Build.Tasks")]
[assembly: AssemblyDescription("MSBuild-based Visual Studio solution file generator")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("SlnGen")]
[assembly: AssemblyCopyright("Copyright ©  2017.  All rights reserved.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: InternalsVisibleTo("SlnGen.Build.Tasks.UnitTests")]