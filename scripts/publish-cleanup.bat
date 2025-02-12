@ECHO off

START "WCIT CLEANUP SCRIPT" /B scripts/cleanup.bat

dotnet publish "Windows Installer.sln" --nologo --self-contained --property:OutputPath=..\build\ --configuration "Release"
