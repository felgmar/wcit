@ECHO OFF

CALL :REMOVEDIR %~dp0..\.vs
CALL :REMOVEDIR %~dp0..\build
CALL :REMOVEDIR %~dp0..\build-installer
CALL :REMOVEDIR %~dp0..\WindowsInstaller.CLI\bin
CALL :REMOVEDIR %~dp0..\WindowsInstaller.CLI\obj
CALL :REMOVEDIR %~dp0..\WindowsInstaller.Core\bin
CALL :REMOVEDIR %~dp0..\WindowsInstaller.Core\obj

:EXITWITHERROR
IF %ERRORLEVEL%==0 (
    EXIT /B
) ELSE (
    ECHO PROGRAM EXITED WITH CODE %ERRORLEVEL%
    EXIT /B %ERRORLEVEL%
)

:REMOVEDIR
    IF EXIST %* (
        ECHO REMOVING DIRECTORY: %*...
        RMDIR /S /Q %*
        CALL :EXITWITHERROR
    ) ELSE (
        ECHO [!] DIRECTORY NOT FOUND: %*
    )
