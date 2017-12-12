# SlnGen
SlnGen is a Visual Studio solution file generator.  Visual Studio solutions generally do not scale well for large project trees.  They are scoped views of a set of projects.  Enterprise-level builds use custom logic like traversal to convey how they should be built by a hosted build environment.  Maintaining Visual Studio solutions becomes hard because you have to keep them in sync with the other build logic.  Instead, SlnGen reads the project references of a given project to create a Visual Studio solution on demand.  For example, you can run it against a unit test project and be presented with a Visual Studio solution containing the unit test project and all of its project references.  You can also run SlnGen against a traversal project in a rooted folder to open a Visual Studio solution containing that view of your project tree.

## Instructions
SlnGen is an MSBuild target so you will need to add a `<PackageReference />` to all projects that you want use it with.  We recommend that you simply add the `PackageReference` to a common import like [Directory.Build.props](https://docs.microsoft.com/en-us/visualstudio/msbuild/customize-your-build#directorybuildprops-example)

```xml
<Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <ItemGroup>
    <PackageReference Include="SlnGen">
      <Version>2.0.0</Version>
    </PackageReference>
  </ItemGroup>
</Project>
```

Once a project is referencing the NuGet package, you launch SlnGen with `MSBuild.exe` by running the `SlnGen` target.

```
C:\Projects\src\ProjectA.UnitTests>MSBuild.exe /Target:SlnGen
Microsoft (R) Build Engine version 15.5.180.51428 for .NET Framework
Copyright (C) Microsoft Corporation. All rights reserved.

Build started 12/11/2017 1:44:55 PM.
Project "C:\Projects\src\ProjectA.UnitTests\ProjectA.UnitTests.csproj" on node 1 (slngen target(s)).
SlnGen:
  Loading project references...
  Loaded 3 project(s)
  Generating Visual Studio solution "C:\Projects\src\ProjectA.UnitTests\ProjectA.UnitTests.sln"...
Done Building Project "C:\Projects\src\ProjectA.UnitTests\ProjectA.UnitTests.sln" (slngen target(s)).


Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:00.46
```
By default, Visual Studio is launched and opens your generated solution file.


## Configuring SlnGen
SlnGen is highly configurable to suite almost any need.

### SlnGenLaunchVisualStudio
Indicates whether or not Visual Studio should be launched to open the solution file after it is generated.

Type: Property

Values: `true` or `false`

Default: `true`

#### Example
Command-line argument
```
MSBuild.exe /Target:SlnGen /Property:"SlnGenLaunchVisualStudio=false"
```
MSBuild property
```xml
<PropertyGroup>
  <SlnGenLaunchVisualStudio>false</SlnGenLaunchVisualStudio>
</PropertyGroup>
```

### SlnGenSolutionFileFullPath
Specifies the full path to the Visual Studio solution file to generate.  By default, the path is the same as the project.

Type: Property
#### Example
Command-line argument
```
MSBuild.exe /Target:SlnGen /Property:"SlnGenSolutionFileFullPath=%TEMP%\MySolution.sln"
```
MSBuild property
```xml
<PropertyGroup>
  <SlnGenSolutionFileFullPath>$(MSBuildProjectDirectory)\$(MSBuildProjectName).sln</SlnGenSolutionFileFullPath>
</PropertyGroup>
```

### SlnGenCustomProjectTypeGuid
Specifies a list of custom project type GUIDs to use when generating the Visual Studio solution.

Type: Item

| Metadata   | Description                                                                                                              |
|-----------------|--------------------------------------------------------------------------------------------------------------------------|
| `ProjectTypeGuid` | Specifies the project type GUID to use in the Visual Studio solution.  It can be in any GUID format that .NET can parse. |

#### Example
```xml
<ItemGroup>
  <SlnGenCustomProjectTypeGuid Include=".myproj">
    <ProjectTypeGuid>{e05dcbd8-5478-4b75-bbdb-3a3a2e743ff2}</ProjectTypeGuid>
  </SlnGenCustomProjectTypeGuid>
  <SlnGenCustomProjectTypeGuid Include=".aproj">
    <ProjectTypeGuid>{bb9d1d44-b292-4016-9ce0-27ea600e8e1c}</ProjectTypeGuid>
  </SlnGenCustomProjectTypeGuid>
</ItemGroup>
```

### SlnGenCollectStats
Indicates whether or not performance statistics for project loading should be collected and logged.  You must specify an MSBuild logger verbosity of at least `Detailed` to see the numbers.

Type: Property

Values: `true` or `false`

Default: `false`

#### Example
Command-line argument
```
MSBuild.exe /Target:SlnGen /Property:"SlnGenCollectStats=true"
```
MSBuild property
```xml
<PropertyGroup>
  <SlnGenCollectStats>true</SlnGenCollectStats>
</PropertyGroup>
```
### SlnGenUseShellExecute
Indicates whether or not the Visual Studio solution file should be opened by the registered file extension handler.  You can disable this setting to use whatever `devenv.exe` is on your `PATH` or you can specify a full path to `devenve.exe` with the `SlnGenDevEnvFullPath` property.

Type: Property

Values: `true` or `false`

Default: `false`

#### Example
Command-line argument
```
MSBuild.exe /Target:SlnGen /Property:"SlnGenUseShellExecute=false"
```
MSBuild property
```xml
<PropertyGroup>
  <SlnGenUseShellExecute>false</SlnGenUseShellExecute>
</PropertyGroup>
```

### SlnGenDevEnvFullPath
Specifies a full path to Visual Studio's `devenv.exe` to use when opening the solution file.  By default, SlnGen will launch the program associated with the `.sln` file extension.  However, in some cases you may want to specify a custom path to Visual Studio.

Type: Property
#### Example
Command-line argument
```
MSBuild.exe /Target:SlnGen /Property:"SlnGenDevEnvFullPath=%VSINSTALLDIR%Common7\IDE\devenv.exe"
```
```xml
<PropertyGroup>
  <SlnGenDevEnvFullPath>$(VSINSTALLDIR)Common7\IDE\devenv.exe</SlnGenDevEnvFullPath>
</PropertyGroup>
```

### InheritGlobalProperties
Indicates whether or not all global variables specified when loading the initial project should be passed around when loading project references.

Type: Property

Values: `true` or `false`

Default: `true`

#### Example
Command-line argument
```
MSBuild.exe /Target:SlnGen /Property:"InheritGlobalProperties=false"
```
MSBuild property
```xml
<PropertyGroup>
  <InheritGlobalProperties>false</InheritGlobalProperties>
</PropertyGroup>
```

### SlnGenGlobalPropertiesToRemove
Specifies a list of inherited global properties to not use when loading projects.

Type: Property
#### Example
Command-line argument
```
MSBuild.exe /Target:SlnGen /Property:"Property1"
```
MSBuild property
```xml
<PropertyGroup>
  <SlnGenGlobalPropertiesToRemove>Property1;Property2</SlnGenGlobalPropertiesToRemove>
</PropertyGroup>
```

### SlnGenGlobalProperties
Specifies MSBuild properties to set when loading projects and project references.  Depending on your needs, you may want to set certain properties when SlnGen is evaluation projects to speed up or control how they are loaded.  The list is specified as a standard set of semicolon delimited key/value pairs separated by an equal sign

Type: Property

Default: `DesignTimeBuild=true;BuildingProject=false`
#### Example
Command-line argument
```
MSBuild.exe /Target:SlnGen /Property:"SlnGenGlobalProperties=DoNotDoSomethingWhenLoadingProjects=true%3BTodayIsYesterday=false"
```
```xml
<PropertyGroup>
  <SlnGenGlobalProperties>DoNotDoSomethingWhenLoadingProjects=true;TodayIsYesterday=false</SlnGenGlobalProperties>
</PropertyGroup>
```
### CustomBeforeSlnGenTargets
Specifies a path to a custom MSBuild file to import before `SlnGen.targets` is imported.  This can be useful to set certain properties, items, or targets before SlnGen's logic is imported.

Type: Property
#### Example
Command-line argument
```
MSBuild.exe /Target:SlnGen /Property:"CustomBeforeSlnGenTargets=MyCustomLogic.targets"
```
```xml
<PropertyGroup>
  <CustomBeforeSlnGenTargets>build\Before.SlnGen.targets</CustomBeforeSlnGenTargets>
</PropertyGroup>
```

### CustomAfterSlnGenTargets
Specifies a path to a custom MSBuild file to import after `SlnGen.targets` is imported.  This can be useful to set certain properties, items, or targets after SlnGen's logic is imported.

Type: Property
#### Example
Command-line argument
```
MSBuild.exe /Target:SlnGen /Property:"CustomAfterSlnGenTargets=MyCustomLogic.targets"
```
```xml
<PropertyGroup>
  <CustomAfterSlnGenTargets>build\After.SlnGen.targets</CustomAfterSlnGenTargets>
</PropertyGroup>
```

