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
    /// <summary>
    /// Represents an instance of Visual Studio.
    /// </summary>
    public sealed class VisualStudioInstance
    {
        private readonly ISetupInstance2 _instance;

        private readonly ConcurrentDictionary<string, Lazy<object>> _lazyValues = new ConcurrentDictionary<string, Lazy<object>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualStudioInstance" /> class.
        /// </summary>
        /// <param name="instance">The <see cref="ISetupInstance2" /> of the instance.</param>
        internal VisualStudioInstance(ISetupInstance2 instance)
        {
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        /// <summary>
        /// Gets a value indicating whether or not the instance of Visual Studio contains MSBuild.
        /// </summary>
        public bool HasMSBuild => GetLazyValue(nameof(HasMSBuild), () => Packages.Any(i => string.Equals(i, "Microsoft.Component.MSBuild", StringComparison.OrdinalIgnoreCase)));

        /// <summary>
        /// Gets the full path to Visual Studio.
        /// </summary>
        public string InstallationPath => GetLazyValue(nameof(InstallationPath), () => _instance.GetInstallationPath());

        /// <summary>
        /// Gets the version of Visual Studio.
        /// </summary>
        public Version InstallationVersion => GetLazyValue(nameof(InstallationVersion), () =>
        {
            if (!Version.TryParse(_instance.GetInstallationVersion(), out Version version))
            {
                version = new Version(1, 0);
            }

            return version;
        });

        /// <summary>
        /// Gets a value indicating whether or not the instance of Visual Studio is Build Tools.
        /// </summary>
        public bool IsBuildTools => GetLazyValue(nameof(IsBuildTools), () => string.Equals(ProductId, "Microsoft.VisualStudio.Product.BuildTools", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Gets an <see cref="IReadOnlyCollection{String}" /> of packages installed in this instance of Visual Studio.
        /// </summary>
        public IReadOnlyCollection<string> Packages => GetLazyValue(nameof(Packages), () => _instance.GetPackages().Select(i => i.GetId()).ToList());

        /// <summary>
        /// Gets an <see cref="ISetupPackageReference" /> for the instance of Visual Studio.
        /// </summary>
        public ISetupPackageReference Product => GetLazyValue(nameof(Product), () => _instance.GetProduct());

        /// <summary>
        /// Gets the product ID for the instance of Visual Studio.
        /// </summary>
        public string ProductId => GetLazyValue(nameof(ProductId), () => Product.GetId());

        private T GetLazyValue<T>(string name, Func<T> func)
        {
            Lazy<object> lazy = _lazyValues.GetOrAdd(name, s => new Lazy<object>(() => func()));

            return (T)lazy.Value;
        }
    }
}