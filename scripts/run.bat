@ECHO off

IF %1.==. (
    ECHO.ERROR: NO PROJECT WAS SPECIFIED
    EXIT /B 1
)

IF %1==CONSOLEAPP (
    pushd CONSOLEAPP
    dotnet run --nologo --self-contained --configuration "Debug"
    popd
) ELSE (
    ECHO.ERROR: %1: INVALID PROJECT
)
