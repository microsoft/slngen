@ECHO OFF
IF "%MSBUILDTOOLSET%" EQU "150" (
    "%~dp0tools\net46\slngen.exe" %SLNGENARGS% %*
    EXIT /B %ERRORLEVEL%
)

IF "%MSBUILDTOOLSET%" GTR "150" (
    "%~dp0tools\net472\slngen.exe" %SLNGENARGS% %*
    EXIT /B %ERRORLEVEL%
)

ECHO SlnGen only supports MSBuild 15 and above, please upgrade your MSBuild.Corext package to the latest.
EXIT /B 1