@ECHO off

SET COMPILER="%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe" /Q
FOR /F "tokens=2 delims=\\" %%A IN ('whoami') DO SET USERNAME="%%A"
SET LICENSE="%~dp0..\LICENSE"
SET REPOSITORYDIR="%~dp0.."
SET OUTPUTDIR="%~dp0..\build-installer"
SET SETUPSCRIPT="%~dp0..\build-installer\wcit-setup.iss"
SET USERNAME="felgmar"
SET VERSION="1.0.1.0"

IF NOT EXIST %LICENSE% (
    ECHO.ERROR: LICENSE FILE NOT FOUND
    EXIT /B 1
)

IF NOT EXIST %OUTPUTDIR% (
    MKDIR %OUTPUTDIR%
)

IF NOT EXIST %SETUPSCRIPT% (
    CALL powershell.exe -ExecutionPolicy Bypass -Command "& {%~dp0download-installer-script.ps1 -OutputPath %SETUPSCRIPT%}"
    CALL icacls.exe "%SETUPSCRIPT%" /grant %USERNAME%:F
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
CALL powershell.exe -ExecutionPolicy Bypass -Command "& {%~dp0patch-installer-script.ps1 -OutputPath %SETUPSCRIPT% -Define AppOutputDir -Value %OUTPUTDIR%}"
CALL powershell.exe -ExecutionPolicy Bypass -Command "& {%~dp0patch-installer-script.ps1 -OutputPath %SETUPSCRIPT% -Define UserName -Value %USERNAME%}"
CALL powershell.exe -ExecutionPolicy Bypass -Command "& {%~dp0patch-installer-script.ps1 -OutputPath %SETUPSCRIPT% -Define AppLicense -Value %LICENSE%}"
CALL powershell.exe -ExecutionPolicy Bypass -Command "& {%~dp0patch-installer-script.ps1 -OutputPath %SETUPSCRIPT% -Define AppVersion -Value %VERSION%}"

ECHO COMPILING INSTALLER...
CALL %COMPILER% %SETUPSCRIPT%

IF %ERRORLEVEL% NEQ 0 (
    ECHO.COMPILATION FAILED WITH CODE %ERRORLEVEL%
    EXIT /B %ERRORLEVEL%
)
