@ECHO off

CALL %~dp0publish-cleanup.bat

IF EXIST %SETUPSCRIPT% (
    DEL /Q %SETUPSCRIPT%
)

CALL %~dp0compile-installer.bat
