@ECHO OFF
SETLOCAL

SET MSBUILD_PROJECT=%~dp0src\SlnGen.Build.Tasks.UnitTests\SlnGen.Build.Tasks.UnitTests.csproj

SET MSBUILD_ARGS=/NoLogo /Restore /Verbosity:Minimal /Target:SlnGen
:args

IF "%~1"=="" GOTO main
IF /I "%~1"=="/nolaunch" SET MSBUILD_ARGS=%MSBUILD_ARGS% /Property:"SlnGenLaunchVisualStudio=false"
IF /I "%~1"=="/diag"     SET MSBUILD_ARGS=%MSBUILD_ARGS% /Property:"SlnGenCollectStats=true" /FileLoggerParameters:"Verbosity=Detailed;PerformanceSummary;LogFile=slngen.log"
SHIFT & GOTO args

:main

where /q msbuild.exe
IF ERRORLEVEL 1 (
    ECHO Could not locate MSBuild.exe.  You must run this command from a Visual Studio Developer Command Prompt
    EXIT /B 1
)

MSBuild.exe %MSBUILD_ARGS% "%MSBUILD_PROJECT%"