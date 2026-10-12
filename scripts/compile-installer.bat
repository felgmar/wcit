@ECHO off

SET "COMPILER=%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe"
IF DEFINED INNO_SETUP_COMPILER SET "COMPILER=%INNO_SETUP_COMPILER%"
FOR /F "tokens=2 delims=\\" %%A IN ('whoami') DO SET USERNAME="%%A"
SET LICENSE="%~dp0..\LICENSE"
SET REPOSITORYDIR="%~dp0.."
SET OUTPUTDIR="%~dp0..\build-installer"
SET SETUPSCRIPT="%~dp0..\build-installer\wcit-setup.iss"
SET USERNAME="felgmar"
SET VERSION="1.0.2.1"
IF DEFINED INNO_SETUP_VERSION SET VERSION="%INNO_SETUP_VERSION%"

IF NOT EXIST "%COMPILER%" GOTO COMPILERNOTFOUND

IF NOT EXIST %LICENSE% (
    ECHO.ERROR: LICENSE FILE NOT FOUND
    EXIT /B 1
)

IF NOT EXIST %OUTPUTDIR% (
    MKDIR %OUTPUTDIR%
)

IF NOT EXIST %SETUPSCRIPT% (
    CALL powershell.exe -ExecutionPolicy Bypass -Command "& {%~dp0download-installer-script.ps1 -OutputPath %SETUPSCRIPT%}"
    IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%
)

FOR %%F IN ("%OUTPUTDIR%\*.exe") DO (
    ECHO REMOVING FILE %%F FROM %OUTPUTDIR%...
    DEL /Q "%OUTPUTDIR%\%%F"
)

IF "%1"=="" IF NOT EXIST %SETUPSCRIPT% (
    ECHO.ERROR: NO INNO SETUP SCRIPT WAS PROVIDED
    EXIT /B 1
)

ECHO PATCHING INNO SETUP SCRIPT...
CALL powershell.exe -ExecutionPolicy Bypass -Command "& {%~dp0patch-installer-script.ps1 -OutputPath %SETUPSCRIPT% -Define RepositoryDir -Value %REPOSITORYDIR%}"
IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%
CALL powershell.exe -ExecutionPolicy Bypass -Command "& {%~dp0patch-installer-script.ps1 -OutputPath %SETUPSCRIPT% -Define AppOutputDir -Value %OUTPUTDIR%}"
IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%
CALL powershell.exe -ExecutionPolicy Bypass -Command "& {%~dp0patch-installer-script.ps1 -OutputPath %SETUPSCRIPT% -Define UserName -Value %USERNAME%}"
IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%
CALL powershell.exe -ExecutionPolicy Bypass -Command "& {%~dp0patch-installer-script.ps1 -OutputPath %SETUPSCRIPT% -Define AppLicense -Value %LICENSE%}"
IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%
CALL powershell.exe -ExecutionPolicy Bypass -Command "& {%~dp0patch-installer-script.ps1 -OutputPath %SETUPSCRIPT% -Define AppVersion -Value %VERSION%}"
IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%

ECHO COMPILING INSTALLER...
CALL "%COMPILER%" /Q %SETUPSCRIPT%

IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%
EXIT /B 0

:COMPILERNOTFOUND
ECHO.ERROR: INNO SETUP COMPILER NOT FOUND: %COMPILER%
EXIT /B 1
