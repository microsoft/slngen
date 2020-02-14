# SlnGen

[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/microsoft.slngen%20CI?branchName=master)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=740&branchName=master)
[![NuGet package](https://img.shields.io/nuget/v/SlnGen.svg)](https://nuget.org/packages/SlnGen)
[![NuGet downloads](https://img.shields.io/nuget/dt/SlnGen.svg)](https://nuget.org/packages/SlnGen)

## Overview
SlnGen is a Visual Studio solution file generator.  Visual Studio solutions generally do not scale well for large project trees.  They are scoped views of a set of projects.  Enterprise-level builds use custom logic like traversal to convey how they should be built by a hosted build environment.  Maintaining Visual Studio solutions becomes hard because you have to keep them in sync with the other build logic.  Instead, SlnGen reads the project references of a given project to create a Visual Studio solution on demand.  For example, you can run it against a unit test project and be presented with a Visual Studio solution containing the unit test project and all of its project references.  You can also run SlnGen against a traversal project in a rooted folder to open a Visual Studio solution containing that view of your project tree.

## Getting Started

Install the package to an individual project (not recommended):

```
 Install-Package Microsoft.VisualStudio.SlnGen
```

Or add it your [Directory.Build.props](https://docs.microsoft.com/en-us/visualstudio/msbuild/customize-your-build#directorybuildprops-example):

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.VisualStudio.SlnGen" Version="<Latest Version>" />
</ItemGroup>
```

Generate and open a Visual Studio solution with MSBuild:

```
> MSBuild /Restore /t:SlnGen
```

You can also create a [DOSKEY](https://en.wikipedia.org/wiki/DOSKEY) alias as a shortcut

```
> doskey slngen=msbuild /nologo /v:m /t:slngen
```

More documentation is available at [https://microsoft.github.io/slngen/](https://microsoft.github.io/slngen/).

# Contributing

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
