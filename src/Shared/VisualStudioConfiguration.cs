// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.VisualStudio.Setup.Configuration;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents a Visual Studio configuration.
    /// </summary>
    internal static class VisualStudioConfiguration
    {
        private static readonly SetupConfiguration SetupConfiguration = new SetupConfiguration();

        /// <summary>
        /// Gets an instance of Visual Studio for the specified path.
        /// </summary>
        /// <param name="path">Any path under Visual Studio.</param>
        /// <returns>A <see cref="VisualStudioInstance" /> if one could be found, otherwise null.</returns>
        public static VisualStudioInstance GetInstanceForPath(string path)
        {
            ISetupInstance2 instance;

            try
            {
                instance = SetupConfiguration.GetInstanceForPath(path) as ISetupInstance2;
            }
            catch (COMException e) when (e.HResult == unchecked((int)0x80070490))
            {
                instance = null;
            }

            return instance == null ? null : new VisualStudioInstance(instance);
        }

        /// <summary>
        /// Gets launchable instances of Visual Studio on the machine.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{VisualStudioInstance}" /> of all launchable instances of Visual Studio.</returns>
        public static IEnumerable<VisualStudioInstance> GetLaunchableInstances()
        {
            int fetched = 1;

            ISetupInstance[] instances = new ISetupInstance[fetched];

            IEnumSetupInstances enumerator = SetupConfiguration.EnumInstances();
            do
            {
                enumerator.Next(fetched, instances, out fetched);

                if (fetched > 0)
                {
                    ISetupInstance2 instance;

                    try
                    {
                        instance = instances[0] as ISetupInstance2;
                    }
                    catch (COMException e) when (e.HResult == unchecked((int)0x80070490))
                    {
                        continue;
                    }

                    if (instance != null)
                    {
                        yield return new VisualStudioInstance(instance);
                    }
                }
            }
            while (fetched > 0);
        }
    }
}