@ECHO off

CALL %~dp0cleanup.bat

dotnet publish "Windows Installer.sln" --nologo --self-contained --property:OutputPath=%~dp0..\build\ --configuration "Release"

MOVE /Y %~dp0..\build\publish %~dp0..\
RMDIR /Q /S %~dp0..\build
MOVE /Y %~dp0..\publish %~dp0..\build

SET COMPILER="%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"

IF %1.=="" (
    echo ERROR: NO INNO SETUP SCRIPT WAS PROVIDED
) ELSE (
    CALL %COMPILER% %1
)
