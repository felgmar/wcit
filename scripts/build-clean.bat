@ECHO off

CALL %~dp0/cleanup.bat

CALL "%~dp0build-solution.bat" Build Debug
exit /b %ERRORLEVEL%
