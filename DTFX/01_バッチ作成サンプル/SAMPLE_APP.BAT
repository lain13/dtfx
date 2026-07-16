@ECHO OFF
SETLOCAL ENABLEDELAYEDEXPANSION
REM ============================================================================
REM ファイル名: SAMPLE_APP.BAT
REM 概要:汎用データ連携サンプルアプリケーション
REM 
REM 例)SAMPLE_APP.BAT
REM 
REM 履歴:
REM  バージョン   日付        作成者      内容
REM  25.1-001-01  2013/08/02  姜  恵遠    新規作成
REM 
REM ============================================================================

REM EXEパス
SET EXEDIRECTORY=.\

REM アプリケーションパス
SET APPDIRECTORY=.\

REM アプリケーションID
SET APPID=SAMPLE_APP

%EXEDIRECTORY%IF.Batch.DTFX.exe -appdirectory %APPDIRECTORY% -appid %APPID%
GOTO END

:END
ENDLOCAL

REM エラーを判定します。エラー発生時には100を返すこととします。正常終了時には0を返すものとします。
ECHO %ERRORLEVEL%

IF ERRORLEVEL 2 EXIT /B 1
IF ERRORLEVEL 1 EXIT /B 100
IF ERRORLEVEL 0 EXIT /B 0

EXIT /B 100

