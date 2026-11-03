@ECHO off

IF "%~1"=="" (
    ECHO ERROR: NO PROJECT WAS SPECIFIED
    EXIT /B 1
)

IF /I "%~1"=="CONSOLEAPP" GOTO RUN_CONSOLEAPP
IF /I "%~1"=="UI" GOTO RUN_UI

ECHO ERROR: %~1: INVALID PROJECT
EXIT /B 1

:RUN_CONSOLEAPP
PUSHD "%~dp0..\ConsoleApp" || EXIT /B 1
dotnet run --nologo --self-contained --configuration "Debug"
SET "EXIT_CODE=%ERRORLEVEL%"
POPD
EXIT /B %EXIT_CODE%

:RUN_UI
PUSHD "%~dp0..\WindowsInstallerUI" || EXIT /B 1
dotnet run --nologo --self-contained --configuration "Debug"
SET "EXIT_CODE=%ERRORLEVEL%"
POPD
EXIT /B %EXIT_CODE%
