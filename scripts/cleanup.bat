@ECHO OFF
SETLOCAL

CALL :REMOVEDIR "%~dp0..\build"
IF ERRORLEVEL 1 EXIT /B 1
CALL :REMOVEDIR "%~dp0..\build-installer"
IF ERRORLEVEL 1 EXIT /B 1
CALL :REMOVEDIR "%~dp0..\ConsoleApp\bin"
IF ERRORLEVEL 1 EXIT /B 1
CALL :REMOVEDIR "%~dp0..\ConsoleApp\obj"
IF ERRORLEVEL 1 EXIT /B 1
CALL :REMOVEDIR "%~dp0..\WindowsInstallerLib\bin"
IF ERRORLEVEL 1 EXIT /B 1
CALL :REMOVEDIR "%~dp0..\WindowsInstallerLib\obj"
IF ERRORLEVEL 1 EXIT /B 1
CALL :REMOVEDIR "%~dp0..\WindowsInstallerUI\bin"
IF ERRORLEVEL 1 EXIT /B 1
CALL :REMOVEDIR "%~dp0..\WindowsInstallerUI\obj"
IF ERRORLEVEL 1 EXIT /B 1
CALL :REMOVEFILE "%~dp0..\buildApp.xbf"
IF ERRORLEVEL 1 EXIT /B 1
CALL :REMOVEFILE "%~dp0..\buildMainWindow.xbf"
IF ERRORLEVEL 1 EXIT /B 1

EXIT /B 0

:REMOVEDIR
IF NOT EXIST "%~1" EXIT /B 0
ECHO REMOVING DIRECTORY: %~1...
RMDIR /S /Q "%~1"
IF EXIST "%~1" (
    ECHO ERROR: Could not remove directory "%~1".
    EXIT /B 1
)
EXIT /B 0

:REMOVEFILE
IF NOT EXIST "%~1" EXIT /B 0
ECHO REMOVING FILE: %~1...
DEL /Q "%~1"
IF EXIST "%~1" (
    ECHO ERROR: Could not remove file "%~1".
    EXIT /B 1
)
EXIT /B 0
