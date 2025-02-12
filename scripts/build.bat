@ECHO off

dotnet build "Windows Installer.sln" --nologo --self-contained --property:OutputPath=..\build\ --configuration "Debug"
