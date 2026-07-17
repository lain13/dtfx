@ECHO OFF
SETLOCAL
SET "SCRIPT_DIR=%~dp0"
SET "APP_DIR=%SCRIPT_DIR:~0,-1%"
SET "DTFX_EXE=%SCRIPT_DIR%..\..\DTFX\bin\Release\IF.Batch.DTFX.exe"

IF NOT EXIST "%DTFX_EXE%" (
  ECHO DTFX has not been built. Build IF.Batch.sln in Release configuration first.
  EXIT /B 1
)

IF NOT EXIST "%SCRIPT_DIR%output" MD "%SCRIPT_DIR%output"

PUSHD "%APP_DIR%" >NUL
"%DTFX_EXE%" -appdirectory "%APP_DIR%" -appid CSV_PIPELINE
SET "EXIT_CODE=%ERRORLEVEL%"
POPD
EXIT /B %EXIT_CODE%
