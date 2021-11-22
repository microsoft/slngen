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

```
Usage: slngen [options] <project path>

Arguments:
  project path                        An optional path to a project which can include wildcards like **\*.csproj or directories which contain projects files. If not specified, all projects in the current directory will be used.

Options:
  -bl|--binarylogger[:<parameters>]   Serializes all build events to a compressed binary file.
                                      By default the file is in the current directory and named "slngen.binlog" and contains the source text of project files, including all imported projects and target files encountered during the build. The optional ProjectImports switch controls this behavior:

                                       ProjectImports=None - Don't collect the project imports.
                                       ProjectImports=Embed - Embed project imports in the log file.
                                       ProjectImports=ZipFile - Save project files to output.projectimports.zip where output is the same name as the binary log file name.

                                      NOTE: The binary logger does not collect non-MSBuild source files such as .cs, .cpp etc.

                                      Example: -bl:output.binlog;ProjectImports=ZipFile
  --collapsefolders <true>            Enables folders containing a single item to be collapsed into their parent folder. Default: false
  -c|--configuration <values>         Specifies one or more Configuration values to use when generating the solution.
  -cl|--consolelogger[:<parameters>]  Parameters to console logger. The available parameters are:
                                          PerformanceSummary--Show time spent in tasks, targets and projects.
                                          Summary--Show error and warning summary at the end.
                                          NoSummary--Don't show error and warning summary at the end.
                                          ErrorsOnly--Show only errors.
                                          WarningsOnly--Show only warnings.
                                          ShowTimestamp--Display the Timestamp as a prefix to any message.
                                          ShowEventId--Show eventId for started events, finished events, and messages
                                          ForceNoAlign--Does not align the text to the size of the console buffer
                                          DisableConsoleColor--Use the default console colors for all logging messages.
                                          ForceConsoleColor--Use ANSI console colors even if console does not support it
                                          Verbosity--overrides the -verbosity setting for this logger.
                                       Example:
                                          --consoleloggerparameters:PerformanceSummary;NoSummary;Verbosity=Minimal
  -vs|--devenvfullpath                Specifies a full path to Visual Studio's devenv.exe to use when opening the solution file. By default, SlnGen will launch the program associated with the .sln file extension.
  -fl|--filelogger[:<parameters>]     Provides any extra parameters for file loggers. The same parameters listed for the console logger are available.
                                      Some additional available parameters are:
                                          LogFile--path to the log file into which the build log will be written.
                                          Append--determines if the build log will be appended to or overwrite the log file.Setting the switch appends the build log to the log file;
                                              Not setting the switch overwrites the contents of an existing log file. The default is not to append to the log file.
                                          Encoding--specifies the encoding for the file, for example, UTF-8, Unicode, or ASCII
                                       Examples:
                                          -fileLoggerParameters:LogFile=MyLog.log;Append;Verbosity=Diagnostic;Encoding=UTF-8
  --folders <true>                    Enables the creation of hierarchical solution folders. Default: false
  --ignoreMainProject                 None of the projects receive special treatment.
  --launch <true|false>               Launch Visual Studio after generating the Solution file. Default: true on Windows
  --loadprojects <false>              When launching Visual Studio, opens the specified solution without loading any projects. Default: true
  --logger                            Use this logger to log events from SlnGen. To specify multiple loggers, specify each logger separately.
                                      The <logger> syntax is:
                                        [<class>,]<assembly>[;<parameters>]
                                      The <logger class> syntax is:
                                        [<partial or full namespace>.]<logger class name>
                                      The <logger assembly> syntax is:
                                        {<assembly name>[,<strong name>] | <assembly file>}
                                      Logger options specify how SlnGen creates the logger. The <logger parameters> are optional, and are passed to the logger exactly as you typed them.
                                      Examples:
                                        -logger:XMLLogger,MyLogger,Version=1.0.2,Culture=neutral
                                        -logger:XMLLogger,C:\Loggers\MyLogger.dll;OutputAsHTML
  --nologo                            Do not display the startup banner and copyright message.
  --nowarn                            Suppress all warning messages
  --platform <values>                 Specifies one or more Platform values to use when generating the solution.
  -p|--property <name=value[;]>       Set or override these project-level properties. <name> is the property name, and <value> is the property value. Use a semicolon or a comma to separate multiple properties, or specify each property separately.
                                        Example:
                                          --property:WarningLevel=2;MyProperty=true
  -d|--solutiondir <path>             An optional path to the directory in which the solution file will be generated. Defaults to the same directory as the project. --solutionfile will take precedence over this switch.
  -o|--solutionfile <path>            An optional path to the solution file to generate. Defaults to the same directory as the project.
  -v|--verbosity                      Display this amount of information in the event log. The available verbosity levels are:
                                        q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic].
  --version                           Display version information only.
  --vsversion                         Specifies that a version of Visual Studio should be included in the solution file. When specified with no value, the value will be set to the version of Visual Studio that is used to open the solution.
  -?|-h|--help                        Show help information.
```

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
| `SlnGenDevEnvFullPath` | Specifies a full path to Visual Studio's `devenv.exe` to use when opening the solution file.  By default, SlnGen will launch the program associated with the `.sln` file extension.  However, in some cases you may want to specify a custom path to Visual Studio. | | |
| `SlnGenGlobalProperties` | Specifies MSBuild properties to set when loading projects and project references. | | `DesignTimeBuild=true;BuildingProject=false` |
| `SlnGenInheritGlobalProperties` | Indicates whether or not all global variables specified when loading the initial project should be passed around when loading project references. | `true` or `false` | `true` |
| `SlnGenGlobalPropertiesToRemove` | Specifies a list of inherited global properties to remove when loading projects. | | |
| `SlnGenBinLog` | Indicates whether or not SlnGen should emit a binary log. | `true` or `false` | `false` |


Command-line argument
```cmd
MSBuild.exe /Target:SlnGen /Property:"SlnGenLaunchVisualStudio=false"
                           /Property:"SlnGenDevEnvFullPath=%VSINSTALLDIR%Common7\IDE\devenv.exe"
```

MSBuild properties
```xml
<PropertyGroup>
  <SlnGenLaunchVisualStudio>false</SlnGenLaunchVisualStudio>
  <SlnGenSolutionFileFullPath>$(MSBuildProjectDirectory)\$(MSBuildProjectName).sln</SlnGenSolutionFileFullPath>
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
| `IncludeInSolutionFile` | Indicates whether or not a project should be included in a generated Solution file. | `true` or `false` | `true` |
| `SlnGenFolders`         | Indicates whether or not a hierarchy of folders should be created.  If `false`, the projects are in a flat list. | `true` or `false` | `true` |
| `SlnGenIsDeployable`    | Indicates whether or not a project is considered deployable by Visual Studio. | `true` or `false` | `false` <br />Service Fabric projects are automatically set to `true` |
| `SlnGenSolutionFolder`  | Specifies a solution folder to place the project in.  `SlnGenFolders` must be `false`. | | |
| `SlnGenProjectName`     | Specifies the display name of the project in the solution. | | Project file name without file extension |

| Item | Description |
|------|-------------|
| `SlnGenSolutionItem` | Specifies a file to include as a Solution Item. |


```xml
<PropertyGroup>
  <!-- Exclude .sqlproj projects from generated solution files -->
  <IncludeInSolutionFile Condition="'$(MSBuildProjectExtension)' == '.sqlproj'">false</IncludeInSolutionFile>

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
* Azure SDK projects (.ccproj)
* Azure Service Fabric projects (.sfproj)
* C# projects (.csproj)
* F# projects (.fsproj)
* Legacy C++ projects (.vcproj)
* Native projects (.nativeProj)
* NuProj projects (.nuproj)
* Scope SDK projects (.scopeproj)
* SQL Server database projects (.sqlproj)
* Visual Basic projects (.vbproj)
* Visual C++ projects (.vcxproj)
* Visual J# projects (.vjsproj)
* WiX projects (.wixproj)

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
