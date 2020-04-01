// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.VisualStudio.SlnGen
{
    internal static class AssemblyResolver
    {
        private static readonly string[] AssemblyExtensions = { ".dll", ".exe" };
        private static string[] _searchPaths;

        public static void Configure(params string[] searchPaths)
        {
            _searchPaths = searchPaths;

            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith("slngen.resources"))
            {
                return null;
            }

            AssemblyName requestedAssemblyName = new AssemblyName(args.Name);

            foreach (FileInfo candidateAssemblyFile in _searchPaths.SelectMany(searchPath => AssemblyExtensions.Select(extension => new FileInfo(Path.Combine(searchPath, $"{requestedAssemblyName.Name}{extension}")))))
            {
                if (!candidateAssemblyFile.Exists)
                {
                    continue;
                }

                AssemblyName candidateAssemblyName = AssemblyName.GetAssemblyName(candidateAssemblyFile.FullName);
                if (requestedAssemblyName.ProcessorArchitecture == ProcessorArchitecture.None && candidateAssemblyName.ProcessorArchitecture != ProcessorArchitecture.MSIL)
                {
                    // The requested assembly has no processor architecture but the candidate assembly does
                    continue;
                }

                if (requestedAssemblyName.ProcessorArchitecture != ProcessorArchitecture.None && requestedAssemblyName.ProcessorArchitecture != candidateAssemblyName.ProcessorArchitecture)
                {
                    // The requested assembly has a processor architecture and the candidate assembly has a different value
                    continue;
                }

                if (requestedAssemblyName.Flags.HasFlag(AssemblyNameFlags.PublicKey) && !requestedAssemblyName.GetPublicKeyToken().SequenceEqual(candidateAssemblyName.GetPublicKeyToken()))
                {
                    // Requested assembly has a public key but it doesn't match the candidate assembly public key
                    continue;
                }

                return Assembly.LoadFrom(candidateAssemblyFile.FullName);
            }

            return null;
        }
    }
}
