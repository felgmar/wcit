@ECHO off

dotnet publish "Windows Installer.sln" --nologo --self-contained --property:OutputPath=..\build\ --configuration "Release"
