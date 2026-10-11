@ECHO off

IF %1.==. (
    ECHO.ERROR: NO PROJECT WAS SPECIFIED
    EXIT /B 1
)

IF %1==WINDOWSINSTALLER.CLI (
    pushd WINDOWSINSTALLER.CLI
    dotnet run --nologo --self-contained --configuration "Debug"
    popd
) ELSE (
    ECHO.ERROR: %1: INVALID PROJECT
)
