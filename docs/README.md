# Overview
SlnGen is a Visual Studio solution file generator.  Visual Studio solutions generally do not scale well for large project trees.  They are scoped views of a set of projects.

Enterprise-level builds use custom logic like traversal to convey how they should be built by a hosted build environment.  Maintaining Visual Studio solutions becomes hard because you have to keep them in sync with the other build logic.

SlnGen reads the project references of a given project to create a Visual Studio solution on demand.  For example, you can run it against a unit test project and be presented with a Visual Studio solution containing the unit test project and all of its project references.  You can also run SlnGen against a traversal project in a rooted folder to open a Visual Studio solution containing that view of your project tree.

# Getting Started
Download and install Visual studio 2019 version 16.4. See [Installation guide](https://docs.microsoft.com/en-us/visualstudio/install/update-visual-studio?view=vs-2019)

SlnGen can be installed as a .NET Core global tool.  To do this, please [install .NET Core 3.0](https://dotnet.microsoft.com/download/dotnet-core/3.0) or above and run the following command:

```cmd
dotnet tool install --global Microsoft.VisualStudio.SlnGen.Tool
```

Once installed, the command `slngen` is added to your `PATH` so its available in any command window.

```cmd
You can invoke the tool using the following command: slngen
Tool 'microsoft.visualstudio.slngen.tool' (version '3.0.37') was successfully installed.
```

See the [frequently asked questions](FAQ) if you are having any issues.

## Command-Line Reference

```cmd
slngen [switches] [project]
```

### Arguments

| Argument | Description |
|----------|-------------|
| <code>project</code> | An optional path to a project to generate a solution file for.  If you don't specify a project file, SlnGen searches the current working directory for a file name extension that ends in proj and uses that file. |

### Switches

| Switch | Short Form | Description |
|--------|------------|-------------|
| <code>--help</code> | <code>-?</code> | Show help information |
| <code>--launch:true&#124;false</code> | | Launch Visual Studio after generating the Solution file. Default: `true` |
| <code>--folders:true&#124;false</code> | | Enables the creation of hierarchical solution folders. Default: `false` |
| <code>--collapsefolders:true&#124;false</code> | | Enables folders containing a single item to be collapsed into their parent folder. Default: `false` |
| <code>--loadprojects:true&#124;false</code> | | When launching Visual Studio, opens the specified solution without loading any projects. Default: `true` |
| <code>--useshellexecute:true&#124;false</code> | <code>-u:true&#124;false</code> | Indicates whether or not the Visual Studio solution file should be opened by the registered file extension handler. Default: `true` |
| <code>--solutionfile:path</code> | `-o:file` | An optional path to the solution file to generate. Defaults to the same directory as the project. |
| <code>--solutiondir:path</code> | `-d:path` | An optional path to the directory in which the solution file will be generated.  Defaults to the same directory as the project. --solutionfile will take precedence over this switch. |
| <code>--devenvpath:path</code> | | Specifies a full path to Visual Studio's devenv.exe to use when opening the solution file. By default, SlnGen will launch the program associated with the .sln file extension. |
| <code>--property:name=value</code> | `-p:name=value` | Set or override these project-level properties. Use a semicolon or a comma to separate multiple properties, or specify each property separately.|
| <code>--configuration:value</code> | | (Optional) Specifies one or more values to use for the solution Configuration (i.e. Debug or Release).  By default, all projects' available values for Configuration are used.  In certain cases, projects do not properly convey the Configuration so it is desirable to generate a solution with your own values. |
| <code>--platform:value</code> | | (Optional) Specifies one or more values to use for the solution Platform (i.e. Any CPU or x64).  By default, all projects' available values for Platform are used.  In certain cases, projects do not properly convey the Platform so it is desirable to generate a solution with your own values. |
| <code>--verbosity:value</code> | `-v:value` | Display this amount of information in the event log. <br />The available verbosity levels are: `q[uiet]`, `m[inimal]`, `n[ormal]`, `d[etailed]`, and `diag[nostic]`.|
| <code>--consolelogger:params</code> | | Parameters to console logger. The available parameters are:<br />&nbsp;&nbsp;`PerformanceSummary`--Show time spent in tasks, targets and projects.<br/>&nbsp;&nbsp;`Summary`--Show error and warning summary at the end.<br/>&nbsp;&nbsp;`NoSummary`--Don't show error and warning summary at the end.<br/>&nbsp;&nbsp;`ErrorsOnly`--Show only errors.<br/>&nbsp;&nbsp;`WarningsOnly`--Show only warnings.<br/>&nbsp;&nbsp;`ShowTimestamp`--Display the Timestamp as a prefix to any message.<br/>&nbsp;&nbsp;`ShowEventId`--Show eventId for started events, finished events, and messages<br/>&nbsp;&nbsp;`ForceNoAlign`--Does not align the text to the size of the console buffer<br/>&nbsp;&nbsp;`DisableConsoleColor`--Use the default console colors for all logging messages.<br/>&nbsp;&nbsp;`ForceConsoleColor`--Use ANSI console colors even if console does not support it<br/>&nbsp;&nbsp;`Verbosity`--overrides the -verbosity setting for this logger.|
| <code>--filelogger[:params]</code> | | Provides any extra parameters for file loggers. The same parameters listed for the console logger are available.<br/>Some additional available parameters are:<br/>&nbsp;&nbsp;`LogFile`--path to the log file into which the build log will be written.<br/>&nbsp;&nbsp;`Append`--determines if the build log will be appended to or overwrite the log file.Setting the switch appends the build log to the log file;<br/>&nbsp;&nbsp;&nbsp;&nbsp;Not setting the switch overwrites the contents of an existing log file. The default is not to append to the log file.<br/>&nbsp;&nbsp;`Encoding`--specifies the encoding for the file, for example, UTF-8, Unicode, or ASCII |
| <code>--binarylogger[:params]</code> | | Serializes all build events to a compressed binary file. By default the file is in the current directory and named `slngen.binlog` and contains the source text of project files, including all imported projects and target files encountered during the build. |
| <code>--logger:params</code> | | Use this logger to log events from SlnGen. To specify multiple loggers, specify each logger separately.<br/>&nbsp;&nbsp;The `<params>` syntax is:<br/>&nbsp;&nbsp;  `[<class>,]<assembly>[;<parameters>]`<br/>&nbsp;&nbsp;The `<logger class>` syntax is:<br/>&nbsp;&nbsp;  `[<partial or full namespace>.]<logger class name>`<br/>&nbsp;&nbsp;The `<logger assembly>` syntax is:<br/>&nbsp;&nbsp;  `{<assembly name>[,<strong name>] | <assembly file>}`<br/>&nbsp;&nbsp;Logger options specify how SlnGen creates the logger. The `<logger parameters>` are optional, and are passed to the logger exactly as you typed them.|
  | <code>--nologo</code> | | Disables writing the SlnGen version and copyright information to the console. |

# Getting Started (MSBuild Target)
SlnGen is an MSBuild target so you will need to add a `<PackageReference />` to all projects that you want use it with.  We recommend that you simply add the `PackageReference` to a common import like [Directory.Build.props](https://docs.microsoft.com/en-us/visualstudio/msbuild/customize-your-build#directorybuildprops-example)

```xml
<ItemGroup>
  <PackageReference Include="SlnGen" Version="3.0.0" />
</ItemGroup>
```

Once a project is referencing the NuGet package, you launch SlnGen with `MSBuild.exe` by running the `SlnGen` target.

```
C:\Projects\src\ProjectA.UnitTests>MSBuild.exe /Target:SlnGen /Verbosity:Minimal /NoLogo
  Loading project references...
  Generating Visual Studio solution "C:\Projects\src\ProjectA.UnitTests\ProjectA.UnitTests.sln"...
```
By default, Visual Studio is launched and opens your generated solution file.


## MSBuild Property Reference
The following properties only apply when using SlnGen as an MSBuild target.

| Property                 | Description                                                                                                | Values             | Default |
|--------------------------|------------------------------------------------------------------------------------------------------------|--------------------|---------|
| `SlnGenLaunchVisualStudio` | Indicates whether or not Visual Studio should be launched to open the solution file after it is generated. | `true` or `false` | `true` |
| `SlnGenSolutionFileFullPath` | Specifies the full path to the Visual Studio solution file to generate.  By default, the path is the same as the project. | | ProjectPath.sln|
| `SlnGenUseShellExecute` | Indicates whether or not the Visual Studio solution file should be opened by the registered file extension handler.  You can disable this setting to use whatever `devenv.exe` is on your `PATH` or you can specify a full path to `devenve.exe` with the `SlnGenDevEnvFullPath` property. | `true` or `false` | `true` |
| `SlnGenDevEnvFullPath` | Specifies a full path to Visual Studio's `devenv.exe` to use when opening the solution file.  By default, SlnGen will launch the program associated with the `.sln` file extension.  However, in some cases you may want to specify a custom path to Visual Studio. | | |
| `SlnGenGlobalProperties` | Specifies MSBuild properties to set when loading projects and project references. | | `DesignTimeBuild=true;BuildingProject=false` |
| `SlnGenInheritGlobalProperties` | Indicates whether or not all global variables specified when loading the initial project should be passed around when loading project references. | `true` or `false` | `true` |
| `SlnGenGlobalPropertiesToRemove` | Specifies a list of inherited global properties to remove when loading projects. | | |


Command-line argument
```cmd
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
  <SlnGenGlobalProperties>DoNotDoSomethingWhenLoadingProjects=true;TodayIsYesterday=false</SlnGenGlobalProperties>
  <SlnGenInheritGlobalProperties>false</SlnGenInheritGlobalProperties>
  <SlnGenGlobalPropertiesToRemove>Property1;Property2</SlnGenGlobalPropertiesToRemove>
</PropertyGroup>
```

# Configuring SlnGen

Read more below to learn how to configure the behavior of SlnGen to fit your needs.

## Customize the Solution File
Use the following properties and items to customize the generated Solution file.

| Property | Description | Values | Default |
|----------|-------------|--------|---------|
| `IncludeInSolutionFile` | Indicates whether or not a project should be included in a generated Solution file.                               | `true` or `false` | `true` |
| `SlnGenFolder`         | If `SlnGenFolders` is false, determines a project's folder. If null or empty, no folder will be created. | `<x:string>` or ` ` | ` ` |
| `SlnGenFolders`         | Indicates whether or not a hierarchy of folders should be created.  If `false`, the projects are in a flat list. | `true` or `false` | `true` |
| `SlnGenIsDeployable`    | Indicates whether or not a project is considered deployable by Visual Studio.                                     | `true` or `false` | `false` <br />Service Fabric projects are automatically set to `true` |

| Item | Description |
|------|-------------|
| `SlnGenSolutionItem` | Specifies a file to include as a Solution Item. |


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

## Custom Project Types
SlnGen knows about the following project types:

To add to this list or override an item, specify an `SlnGenCustomProjectTypeGuid` item in your projects.

| Item | Description |
|------|-------------|
| `SlnGenCustomProjectTypeGuid` | Specifies a file extension and project type GUID.  The format of the extension must be ".ext" |


| Metadata | Description |
|----------|-------------|
| `ProjectTypeGuid` | Specifies the project type GUID to use in the Visual Studio solution.  It can be in any GUID format that .NET can parse. |

```xml
<ItemGroup>
  <SlnGenCustomProjectTypeGuid Include=".myproj" ProjectTypeGuid="{e05dcbd8-5478-4b75-bbdb-3a3a2e743ff2}" />
  <SlnGenCustomProjectTypeGuid Include=".aproj" ProjectTypeGuid="{bb9d1d44-b292-4016-9ce0-27ea600e8e1c}" />
</ItemGroup>
```

## Extensibility

You can extend SlnGen with the following properties:

| Property                 | Description                                                                                                |
|--------------------------|------------------------------------------------------------------------------------------------------------|
| CustomBeforeSlnGenTargets | Specifies a path to a custom MSBuild file to import **before** `SlnGen.targets` is imported.  This can be useful to set certain properties, items, or targets before SlnGen's logic is imported.|
| CustomAfterSlnGenTargets | Specifies a path to a custom MSBuild file to import **after** `SlnGen.targets` is imported.  This can be useful to set certain properties, items, or targets after SlnGen's logic is imported. |
