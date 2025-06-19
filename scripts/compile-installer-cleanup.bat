@ECHO off

CALL %~dp0publish-cleanup.bat

SET COMPILER="%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" /Q
FOR /F "tokens=2 delims=\\" %%A IN ('whoami') DO SET USERNAME="%%A"
SET LICENSE="%~dp0..\LICENSE"
SET OUTPUTDIR="%~dp0..\build-installer"
SET SETUPSCRIPT="%~dp0wcit-setup.iss"
SET USERNAME="felgmar"

IF NOT EXIST %LICENSE% (
    ECHO.ERROR: LICENSE FILE NOT FOUND
    EXIT /B 1
)

IF EXIST %SETUPSCRIPT% (
    DEL /Q %SETUPSCRIPT%
)

IF NOT EXIST %SETUPSCRIPT% (
    CALL powershell.exe -ExecutionPolicy Bypass -Command "& {%~dp0download-installer-script.ps1 -OutputPath %SETUPSCRIPT%}"
    CALL icacls.exe "%SETUPSCRIPT%" /grant %USERNAME%:F
)

IF NOT EXIST %OUTPUTDIR% (
    MKDIR %OUTPUTDIR%
)

FOR /F "tokens=* delims=.exe" %%F IN ('DIR /A /B "%OUTPUTDIR%"') DO (
    ECHO REMOVING FILE %%F FROM %OUTPUTDIR%...
    DEL /Q "%OUTPUTDIR%\%%F"
)

IF "%1"=="" IF NOT EXIST %SETUPSCRIPT% (
    ECHO.ERROR: NO INNO SETUP SCRIPT WAS PROVIDED
)

ECHO PATCHING INNO SETUP SCRIPT...
CALL powershell.exe -ExecutionPolicy Bypass -Command "& {%~dp0patch-installer-script.ps1 -OutputPath %SETUPSCRIPT% -Define AppOutputDir -Value %OUTPUTDIR%}"
CALL powershell.exe -ExecutionPolicy Bypass -Command "& {%~dp0patch-installer-script.ps1 -OutputPath %SETUPSCRIPT% -Define UserName -Value %USERNAME%}"

ECHO COMPILING INSTALLER...
CALL %COMPILER% %SETUPSCRIPT%

IF %ERRORLEVEL% NEQ 0 (
    ECHO.COMPILATION FAILED WITH CODE %ERRORLEVEL%
    EXIT /B %ERRORLEVEL%
)
