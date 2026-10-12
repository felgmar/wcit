@ECHO off

CALL %~dp0cleanup.bat

CALL "%~dp0build-solution.bat" Publish Release
if errorlevel 1 exit /b %ERRORLEVEL%

MOVE /Y %~dp0..\build\publish %~dp0..\
RMDIR /Q /S %~dp0..\build
MOVE /Y %~dp0..\publish %~dp0..\build
