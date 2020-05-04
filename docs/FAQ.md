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
You can leave projects out of the solution by setting the MSBuild property `ShouldIncludeInSolution`.  This can be set in invdividual projects like this:

```xml
<PropertyGroup>
  <ShouldIncludeInSolution>false</ShouldIncludeInSolution>
</PropertyGroup>
```

You can also set this property in a common import like `Directory.Build.props` with a condition:

```xml
<PropertyGroup>
  <ShouldIncludeInSolution Condition="'$(MSBuildProjectExtension)' == '.myproj'">false</ShouldIncludeInSolution>
</PropertyGroup>
```

This would leave out any project with an extension `.myproj` since Visual Studio can't load that kind of project anyway.

## How do I troubleshoot SlnGen?
You can generate a diagnostic log by specifying the `--binarylogger` command-line parameter:

```cmd
slngen --binarylogger
```

This will generate an `slngen.binlog` that can be opened with the [MSBuild Structured Log Viewer](https://msbuildlog.com).  The log will show the evaluation of each project, its properties and its items.  This can help determine why things are not working as expected.

![[MSBuild Structured Log Viewer](https://msbuildlog.com)](https://msbuildlog.com/Screenshot1.png)

With the binary log, ensure that the projects aren't missing any imports and they declare `<ProjectReference />` items.

## How does I get support for SlnGen?
Please search for existing issues to [see if someone has already reported the issue](https://github.com/microsoft/slngen/issues) and if there is a workaround or solution.  If not, please open a [new issue](https://github.com/microsoft/slngen/issues/new) to be investigated.

If possible, include a binary log.  If not, please include the following:
* Version of SlnGen
* Command-line arguments
* Link to your repository
