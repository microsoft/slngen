# Frequently Asked Questions

## How does SlnGen work?
SlnGen does the following:
1. Calls the [MSBuild Static Graph API](https://github.com/Microsoft/msbuild/blob/master/documentation/specs/static-graph.md) to load projects and their transitive references.
2. Map project extension to [Visual Studio Project Type GUID](https://www.codeproject.com/reference/720512/list-of-visual-studio-project-type-guids)
3. Write out a [Visual Studio solution file](https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/solution-dot-sln-file)
4. Launches Visual Studio

## Why is Visual Studio not working with my generated solution files?
SlnGen does its best to correctly map projects with their corresponding project system based on the file extension.  If you have a project with a `.csproj` extension but its not really a C# Project, then Visual Studio can have trouble loading the project.

Also, if you have a custom project like `build.proj`, it probably won't load properly in Visual Studio.  It is best to leave this projects out of generated solutions as they can cause issues.

SlnGen does not interact with Visual Studio at all, so if Visual Studio is having issues its most likely not related to SlnGen.

## Why doesn't SlnGen load my projects?
SlnGen uses the standard MSBuild API to evaluate projects.  If the project contains invalid MSBUild project XML or custom build logic prevents them from being loaded, then SlnGen will not work properly.  Ensure that your projects can be evaluated before using SlnGen.

## How do I control the Solution Configuration (Platforms and Configurations)
Visual Studio and SlnGen determine the values for Platform and Configuration based on declared values in your project.

For "legacy" projects, it looks at conditions and their proposed values:
```xml
<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
  ...
</PropertyGroup>
<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ">
  ...
</PropertyGroup>
```
In the example above, the project supports a Configuration of Debug or Release and a Platform of AnyCPU and so the generated solution will have these values.

Newer SDK-style projects use the `Configurations` and `Platforms` properties:
```xml
<PropertyGroup>
  <Platforms>AnyCPU</Platforms>
  <Configurations>Debug;Release</Configurations>
</PropertyGroup>
```

If you find that the solution does not contain the values you desire, consider updating the projects to properly declare what they target.  In some cases you may not be declaring these values correctly or you have a custom value.  To generate solutions with your own values at the command-line, use the `--platform` and `--configuration` arguments:

```cmd
slgen --platform x64 --configuration Debug;Release
```

This tells SlnGen to ignore the values in your projects and to generate a solution with just that combination.  You can also specify your custom values:
```cmd
slgen --platform x64;x86 --configuration MyCustomConfiguration
```

If you're using the MSBuild target, please specify the supported Platforms and Configurations if possible.  To override them when generating a solution, you'll need to specify a value for `SlnGenGlobalProperties` in your `Directory.Build.props` or individual projects:

```xml
<PropertyGroup>
  <SlnGenGlobalProperties>Platform=64</SlnGenGlobalProperties>
</PropertyGroup>
```

## Why are my project references missing?
The standard convention for declare project dependencies is using `<ProjectReference />` items.  Since SlnGen uses an MSBuild API to evaluate projects and their dependencies, your projects must follow this pattern to be recursively discovered.

> If a project reference is missing, make sure you see a project reference to it; for example:
> 
> ```xml
> <ProjectReference Include="..\MyLibrary\MyLibrary.csproj" />
> ```


> If you only have an assembly reference, **SlnGen will not be able to map it to a project reference**.
>```xml
> <Reference Include="..\MyLibrary\bin\Debug\MyLibrary.dll" />
> ```

## How do I leave projects out of the solution?
You can leave projects out of the solution by setting the MSBuild property `IncludeInSolutionFile`.  This can be set in invdividual projects like this:

```xml
<PropertyGroup>
  <IncludeInSolutionFile>false</IncludeInSolutionFile>
</PropertyGroup>
```

You can also set this property in a common import like `Directory.Build.props` with a condition:

```xml
<PropertyGroup>
  <IncludeInSolutionFile Condition="'$(MSBuildProjectExtension)' == '.myproj'">false</IncludeInSolutionFile>
</PropertyGroup>
```

This would leave out any project with an extension `.myproj` since Visual Studio can't load that kind of project anyway.

## How can I detect that a project is being loaded by SlnGen?
SlnGen sets a global property when loading projects, `IsSlnGen`, to the value `true`.  This allows you to set conditions to control logic.

For example, you want to leave out a `ProjectReference` because you know it won't load correctly in Visual Studio:
```xml
<ItemGroup>
  <ProjectReference Include="..." Condition="'$(IsSlnGen)' != 'true'" />
</ItemGroup>
```

## How do I troubleshoot SlnGen?
You can generate a diagnostic log by specifying the `--binarylogger` command-line parameter:

```cmd
slngen --binarylogger
```

This will generate an `slngen.binlog` that can be opened with the [MSBuild Structured Log Viewer](https://msbuildlog.com).  The log will show the evaluation of each project, its properties and its items.  This can help determine why things are not working as expected.

![[MSBuild Structured Log Viewer](https://msbuildlog.com)](https://msbuildlog.com/Screenshot1.png)

With the binary log, ensure that the projects aren't missing any imports and they declare `<ProjectReference />` items.

## How do I get support for SlnGen?
Please search for existing issues to [see if someone has already reported the issue](https://github.com/microsoft/slngen/issues) and if there is a workaround or solution.  If not, please open a [new issue](https://github.com/microsoft/slngen/issues/new) to be investigated.

If possible, include a binary log.  If not, please include the following:
* Version of SlnGen
* Command-line arguments
* Link to your repository
