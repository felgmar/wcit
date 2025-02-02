@ECHO off

START "WCIT CLEANUP SCRIPT" /B scripts/cleanup.bat

dotnet build "Windows Installer.sln" --nologo --self-contained --property:OutputPath=..\build\ --configuration "Debug"
