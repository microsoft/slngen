// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.
using Microsoft.VisualStudio.Setup.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.VisualStudio.SlnGen
{
    public sealed class VisualStudioInstance
    {
        private readonly ISetupInstance2 _instance;

        private readonly ConcurrentDictionary<string, Lazy<object>> _lazyValues = new ConcurrentDictionary<string, Lazy<object>>(StringComparer.OrdinalIgnoreCase);

        internal VisualStudioInstance(ISetupInstance2 instance)
        {
            _instance = instance;
        }

        public string Description => GetLazyValue(nameof(Description), () => _instance.GetDescription());

        public string DisplayName => GetLazyValue(nameof(DisplayName), () => _instance.GetDisplayName());

        public string InstallationName => GetLazyValue(nameof(InstallationName), () => _instance.GetInstallationName());

        public string InstallationPath => GetLazyValue(nameof(InstallationPath), () => _instance.GetInstallationPath());

        public IReadOnlyCollection<string> Packages => GetLazyValue(nameof(Packages), () => _instance.GetPackages().Select(i => i.GetId()).ToList());

        public bool HasMSBuild => GetLazyValue(nameof(HasMSBuild), () => Packages.Any(i => string.Equals(i, "Microsoft.Component.MSBuild", StringComparison.OrdinalIgnoreCase)));

        public Version InstallationVersion => GetLazyValue(nameof(InstallationVersion), () =>
        {
            if (!Version.TryParse(_instance.GetInstallationVersion(), out Version version))
            {
                version = new Version(1, 0);
            }

            return version;
        });

        public ISetupPackageReference Product => GetLazyValue(nameof(Product), () => _instance.GetProduct());

        public string ProductId => GetLazyValue(nameof(ProductId), () => Product.GetId());

        public bool IsBuildTools => GetLazyValue(nameof(IsBuildTools), () => string.Equals(ProductId, "Microsoft.VisualStudio.Product.BuildTools", StringComparison.OrdinalIgnoreCase));

        private T GetLazyValue<T>(string name, Func<T> func)
        {
            Lazy<object> lazy = _lazyValues.GetOrAdd(name, s => new Lazy<object>(() => func()));

            return (T)lazy.Value;
        }
    }
}
