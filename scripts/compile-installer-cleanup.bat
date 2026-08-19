@ECHO OFF

SET SETUPSCRIPT="%~dp0..\build-installer\wcit-setup.iss"

CALL %~dp0publish-cleanup.bat

IF EXIST %SETUPSCRIPT% (
    DEL /Q %SETUPSCRIPT%
)

CALL %~dp0compile-installer.bat
