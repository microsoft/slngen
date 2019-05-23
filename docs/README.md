# Overview
SlnGen is a Visual Studio solution file generator.  Visual Studio solutions generally do not scale well for large project trees.  They are scoped views of a set of projects.

Enterprise-level builds use custom logic like traversal to convey how they should be built by a hosted build environment.  Maintaining Visual Studio solutions becomes hard because you have to keep them in sync with the other build logic.

SlnGen reads the project references of a given project to create a Visual Studio solution on demand.  For example, you can run it against a unit test project and be presented with a Visual Studio solution containing the unit test project and all of its project references.  You can also run SlnGen against a traversal project in a rooted folder to open a Visual Studio solution containing that view of your project tree.

# Getting Started
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
C:\Projects\src\ProjectA.UnitTests>MSBuild.exe /Target:SlnGen /Verbosity:Minimal /NoLogo
  Loading project references...
  Generating Visual Studio solution "C:\Projects\src\ProjectA.UnitTests\ProjectA.UnitTests.sln"...
```
By default, Visual Studio is launched and opens your generated solution file.

# Configuring SlnGen

Read more below to learn how to configure the behavior of SlnGen to fit your needs.

## Customize the Solution File
Use the following properties and items to customize the generated Solution file.

| Property                 | Description                                                                                                | Values             | Default |
|-----------------------------|--------------------------------------------------|
| IncludeInSolutionFile | Indicates whether or not a project should be included in a generated Solution file. | `true` or `false` | `true` |
| SlnGenFolders | Indicates whether or not a hierarchy of folders should be created.  If `false`, the projects are in a flat list. | `true` or `false` | `true` |

| Item                        | Description                                      |
|-----------------------------|--------------------------------------------------|
| SlnGenSolutionItem | Specifies a file to include as a Solution Item. |


```xml
<PropertyGroup>
  <!-- Exclude .sqlproj projects from generated solution files -->
  <IncludeInSolutionFile Condition="'$(MSBuildProjectExtension)' == '.sqlproj'">false</SlnGenLaunchVisualStudio>

  <!-- Disable folder hierarchy in Solution files, projects will be in a flat list instead -->
  <SlnGenFolders>false</SlnGenFolders>
</PropertyGroup>

<ItemGroup>
  <SlnGenSolutionItem Include="$(MSBuildThisFileDirectory)global.json" />
  <SlnGenSolutionItem Include="$(MSBuildThisFileDirectory)README.md" />
</ItemGroup>
```

## Launching of Visual Studio


| Property                 | Description                                                                                                | Values             | Default |
|--------------------------|------------------------------------------------------------------------------------------------------------|--------------------|---------|
| SlnGenLaunchVisualStudio | Indicates whether or not Visual Studio should be launched to open the solution file after it is generated. | `true` or `false` | `true` |
| SlnGenSolutionFileFullPath | Specifies the full path to the Visual Studio solution file to generate.  By default, the path is the same as the project. | | ProjectPath.sln|
| SlnGenUseShellExecute | Indicates whether or not the Visual Studio solution file should be opened by the registered file extension handler.  You can disable this setting to use whatever `devenv.exe` is on your `PATH` or you can specify a full path to `devenve.exe` with the `SlnGenDevEnvFullPath` property. | `true` or `false` | `true` |
| SlnGenDevEnvFullPath | Specifies a full path to Visual Studio's `devenv.exe` to use when opening the solution file.  By default, SlnGen will launch the program associated with the `.sln` file extension.  However, in some cases you may want to specify a custom path to Visual Studio. | | |


Command-line argument
```
MSBuild.exe /Target:SlnGen /Property:"SlnGenLaunchVisualStudio=false"
                           /Property:"SlnGenUseShellExecute=false"
                           /Property:"SlnGenDevEnvFullPath=%VSINSTALLDIR%Common7\IDE\devenv.exe"
```

MSBuild properties
```xml
<PropertyGroup>
  <SlnGenLaunchVisualStudio>false</SlnGenLaunchVisualStudio>
  <SlnGenSolutionFileFullPath>$(MSBuildProjectDirectory)\$(MSBuildProjectName).sln</SlnGenSolutionFileFullPath>
  <SlnGenUseShellExecute>false</SlnGenUseShellExecute>
</PropertyGroup>
```

## Custom Project Types
SlnGen knows about the following project types:

To add to this list or override an item, specify an `SlnGenCustomProjectTypeGuid` item in your projects.

| Item                        | Description                                      |
|-----------------------------|--------------------------------------------------|
| SlnGenCustomProjectTypeGuid | Specifies a file extension and project type GUID.  The format of the extension must be ".ext" |


| Metadata        | Description                                                                                                              |
|-----------------|--------------------------------------------------------------------------------------------------------------------------|
| ProjectTypeGuid | Specifies the project type GUID to use in the Visual Studio solution.  It can be in any GUID format that .NET can parse. |

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

## Evaluation Properties

MSBuild can set properties that effect how a project is loaded.  Setting certain properties can dramatically speed up the evaluation time of projects by excluding certain imports or items.  You can use the following properties to configure how SlnGen loads your projects:

| Property                 | Description                                                                                                | Values             | Default |
|--------------------------|------------------------------------------------------------------------------------------------------------|--------------------|---------|
| SlnGenGlobalProperties | Specifies MSBuild properties to set when loading projects and project references. | | `DesignTimeBuild=true;BuildingProject=false` |
| SlnGenInheritGlobalProperties | Indicates whether or not all global variables specified when loading the initial project should be passed around when loading project references. | `true` or `false` | `true` |
| SlnGenGlobalPropertiesToRemove | Specifies a list of inherited global properties to remove when loading projects. | | |

Command-line argument
```
MSBuild.exe /Target:SlnGen /Property:"SlnGenGlobalProperties=DoNotDoSomethingWhenLoadingProjects=true%3BTodayIsYesterday=false" 
                           /Property:"SlnGenInheritGlobalProperties=false" 
                           /Property:"SlnGenGlobalPropertiesToRemove=MyProperty%3BMyOtherProperty"
```
MSBuild property
```xml
<PropertyGroup>
  <SlnGenGlobalProperties>DoNotDoSomethingWhenLoadingProjects=true;TodayIsYesterday=false</SlnGenGlobalProperties>
  <InheritGlobalProperties>false</InheritGlobalProperties>
  <SlnGenGlobalPropertiesToRemove>Property1;Property2</SlnGenGlobalPropertiesToRemove>
</PropertyGroup>
```


## Extensibility

You can extend SlnGen with the following properties:

| Property                 | Description                                                                                                |
|--------------------------|------------------------------------------------------------------------------------------------------------|
| CustomBeforeSlnGenTargets | Specifies a path to a custom MSBuild file to import **before** `SlnGen.targets` is imported.  This can be useful to set certain properties, items, or targets before SlnGen's logic is imported.|
| CustomAfterSlnGenTargets | Specifies a path to a custom MSBuild file to import **after** `SlnGen.targets` is imported.  This can be useful to set certain properties, items, or targets after SlnGen's logic is imported. |

Command-line argument
```
MSBuild.exe /Target:SlnGen /Property:"CustomBeforeSlnGenTargets=MyCustomLogic.targets"
                           /Property:"CustomAfterSlnGenTargets=MyCustomLogic.targets"
```
```xml
<PropertyGroup>
  <CustomBeforeSlnGenTargets>build\Before.SlnGen.targets</CustomBeforeSlnGenTargets>
  <CustomAfterSlnGenTargets>build\After.SlnGen.targets</CustomAfterSlnGenTargets>
</PropertyGroup>
```

## Troubleshooting

SlnGen runs as a standard MSBuild target.  You must increase the logging verbosity of MSBuild.exe to see more diagnostic logging.  We recommend that you use [MSBuild Binary Logger](http://msbuildlog.com/).

| Property                 | Description                                                                                                | Values             | Default |
|--------------------------|------------------------------------------------------------------------------------------------------------|--------------------|---------|
| SlnGenCollectStats | If your projects are loading slowly, SlnGen can log a performance summary to help you understand why.  You must specify an MSBuild logger verbosity of at least `Detailed` to see the summary in a log. | `true` or `false` | `false` |


Log to a file named `msbuild.log`
```
MSBuild.exe /Target:SlnGen /Property:"SlnGenCollectStats=true" /FileLoggerParameters:Verbosity=Detailed
```

Log to binary log named `msbuild.binlog` viewable in the [MSBuild Structured Log Viewer](http://msbuildlog.com/)
```
MSBuild.exe /Target:SlnGen /Property:"SlnGenCollectStats=true" /BinaryLogger
```

MSBuild properties
```xml
<PropertyGroup>
  <SlnGenCollectStats>true</SlnGenCollectStats>
</PropertyGroup>
```