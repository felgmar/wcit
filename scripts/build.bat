@ECHO off

CALL "%~dp0build-solution.bat" Build Debug
exit /b %ERRORLEVEL%
