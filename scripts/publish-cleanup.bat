@ECHO off

CALL %~dp0cleanup.bat

dotnet publish "Windows Installer.sln" --nologo --self-contained --property:OutputPath=%~dp0..\build\;UseSharedCompilation=false --configuration "Release"

MOVE /Y %~dp0..\build\publish %~dp0..\
RMDIR /Q /S %~dp0..\build
MOVE /Y %~dp0..\publish %~dp0..\build
