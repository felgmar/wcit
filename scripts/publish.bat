@ECHO off

CALL "%~dp0build-solution.bat" Publish Release
exit /b %ERRORLEVEL%
