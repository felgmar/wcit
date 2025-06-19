@ECHO off

CALL %~dp0/cleanup.bat

dotnet build "Windows Installer.sln" --nologo --self-contained --property:OutputPath=%~dp0..\build\;UseSharedCompilation=false --configuration "Debug"
