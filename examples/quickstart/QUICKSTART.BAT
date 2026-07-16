@ECHO OFF
SETLOCAL
SET "SCRIPT_DIR=%~dp0"
SET "DTFX_EXE=%SCRIPT_DIR%..\..\DTFX\bin\Release\IF.Batch.DTFX.exe"

IF NOT EXIST "%DTFX_EXE%" (
  ECHO DTFX has not been built. Build IF.Batch.sln in Release configuration first.
  EXIT /B 1
)

"%DTFX_EXE%" -appdirectory "%SCRIPT_DIR%" -appid QUICKSTART
EXIT /B %ERRORLEVEL%
