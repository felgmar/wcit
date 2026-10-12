@ECHO off
setlocal EnableDelayedExpansion

set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "!VSWHERE!" goto no_vswhere

set "MSBUILD="
for /f "usebackq tokens=*" %%i in (`"!VSWHERE!" -latest -products * -requires Microsoft.Component.MSBuild -property installationPath`) do set "MSBUILD=%%i\MSBuild\Current\Bin\MSBuild.exe"
if not exist "!MSBUILD!" goto no_msbuild

set "PUBLISH_PROPERTY="
if /i "%~1"=="Publish" set "PUBLISH_PROPERTY=-p:PublishDir="%~dp0..\build\publish""
set "UI_VERSION_PROPERTY="
if defined VERSION set "UI_VERSION_PROPERTY=-p:WindowsInstallerUIVersion=!VERSION!"

"%MSBUILD%" "%~dp0..\Windows Installer.slnx" -restore -p:Platform=x64 -p:Configuration=%~2 -p:OutputPath="%~dp0..\build" -p:SelfContained=true -target:%~1 -verbosity:minimal !PUBLISH_PROPERTY! !UI_VERSION_PROPERTY!
exit /b %ERRORLEVEL%

:no_vswhere
ECHO Could not find Visual Studio Installer (vswhere.exe). Install Visual Studio with the WinUI application development workload.
exit /b 1

:no_msbuild
ECHO Could not locate Visual Studio MSBuild. Install Visual Studio with the WinUI application development workload.
exit /b 1
