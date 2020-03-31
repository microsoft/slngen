// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.VisualStudio.Setup.Configuration;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.SlnGen
{
    internal sealed class VisualStudioConfiguration
    {
        private readonly SetupConfiguration _configuration = new SetupConfiguration();

        public VisualStudioInstance GetInstanceForPath(string path) => GetInstance(() => _configuration.GetInstanceForPath(path));

        public IEnumerable<VisualStudioInstance> GetLaunchableInstances() => EnumInstances(_configuration.EnumInstances());

        private IEnumerable<VisualStudioInstance> EnumInstances(IEnumSetupInstances enumerator)
        {
            int fetched = 1;

            ISetupInstance[] instances = new ISetupInstance[fetched];

            do
            {
                enumerator.Next(fetched, instances, out fetched);

                if (fetched > 0)
                {
                    yield return new VisualStudioInstance(instances[0] as ISetupInstance2);
                }
            }
            while (fetched > 0);
        }

        private VisualStudioInstance GetInstance(Func<ISetupInstance> getInstanceFunc)
        {
            ISetupInstance2 instance;

            try
            {
                instance = getInstanceFunc() as ISetupInstance2;
            }
            catch (COMException e) when (e.HResult == unchecked((int)0x80070490))
            {
                instance = null;
            }

            return instance == null ? null : new VisualStudioInstance(instance);
        }
    }
}