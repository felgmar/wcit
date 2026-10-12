@ECHO OFF

CALL "%~dp0cleanup.bat"
IF ERRORLEVEL 1 EXIT /B %ERRORLEVEL%

CALL "%~dp0build-solution.bat" Publish Release
EXIT /B %ERRORLEVEL%
