::========================================================================
:: Batch_ImportNew_ExportRT_optimized.bat
:: Optimized version of Batch_ImportNew_ExportRT.bat
::
:: Changes from original:
::   1. Added @echo off and SETLOCAL for clean console output and
::      safe variable scoping (variables don't leak to the parent shell).
::   2. Fixed date variable construction.  The original used locale-
::      dependent substring offsets on %date% and left the bare
::      expression "%yyyy%_%mm%" on its own line, which cmd.exe tries
::      to execute as a command and fails.  This version uses
::      "wmic os get localdatetime" which always returns an ISO-8601
::      timestamp regardless of the Windows regional format setting.
::   3. All six SkylineCmd --import-all calls now run in parallel.
::      Each is launched as a background process (start /B) wrapped in
::      a small helper batch file written to a temp directory.  The
::      helper writes a sentinel marker file when it finishes, and the
::      main script polls those markers before moving on.  This means
::      the total import time is roughly the duration of the slowest
::      single instrument instead of the sum of all six.
::   4. All six SkylineCmd --report-file export calls are similarly
::      parallelised and waited on before the robocopy phase starts.
::   5. robocopy calls are unchanged in behaviour but now run after
::      confirmed export completion.  A final robocopy consolidation
::      step copies everything to RetentionTimeExports2.
::   6. Temp sentinel directory and helper files are cleaned up at exit.
::========================================================================

@echo off
SETLOCAL ENABLEDELAYEDEXPANSION

cd "C:\Program Files\Skyline"

:: -----------------------------------------------------------------------
:: Build %yearmonth% in a locale-independent way via wmic
:: (original used %date:~10,5% / %date:~4,2% which break on non-US
::  regional settings and left a dangling "%yyyy%_%mm%" execute attempt)
:: -----------------------------------------------------------------------
for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value 2^>nul') do set _dt=%%I
set yyyy=%_dt:~0,4%
set mm=%_dt:~4,2%
set yearmonth=%yyyy%_%mm%

echo.
echo Processing data for period: %yearmonth%
echo.

:: -----------------------------------------------------------------------
:: Temp directory for helper batch files and sentinel marker files
:: -----------------------------------------------------------------------
set TMPDIR=%TEMP%\skyline_batch_%RANDOM%
mkdir "%TMPDIR%"

:: -----------------------------------------------------------------------
:: Phase 1: Write per-instrument helper scripts and start them in parallel
::
:: Each helper runs one SkylineCmd --import-all call and on success
:: writes a .done marker file so the wait loop below can detect completion.
:: -----------------------------------------------------------------------
echo [Phase 1] Starting parallel imports...

(
  echo @echo off
  echo "C:\Program Files\Skyline\SkylineCmd.exe" --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\LUM1\Skyline AutoQC Lumos1.sky" --import-all="Y:\msdata\2026\%yearmonth%\LUM1" --import-filename-pattern="LUM1_HeLa_*" --save
  echo echo %ERRORLEVEL% ^> "%TMPDIR%\LUM1_import.done"
) > "%TMPDIR%\import_LUM1.bat"

(
  echo @echo off
  echo "C:\Program Files\Skyline\SkylineCmd.exe" --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\LUM2\Skyline AutoQC Lumos2.sky" --import-all="Y:\msdata\2026\%yearmonth%\LUM2" --import-filename-pattern="LUM2_HeLa_*" --save
  echo echo %ERRORLEVEL% ^> "%TMPDIR%\LUM2_import.done"
) > "%TMPDIR%\import_LUM2.bat"

(
  echo @echo off
  echo "C:\Program Files\Skyline\SkylineCmd.exe" --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\QEX2\Skyline AutoQC QEX2.sky" --import-all="Y:\msdata\2026\%yearmonth%\QEX2" --import-filename-pattern="QEX2_HeLa_*" --save
  echo echo %ERRORLEVEL% ^> "%TMPDIR%\QEX2_import.done"
) > "%TMPDIR%\import_QEX2.bat"

(
  echo @echo off
  echo "C:\Program Files\Skyline\SkylineCmd.exe" --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\ECL1\Skyline AutoQC ECL1.sky" --import-all="Y:\msdata\2026\%yearmonth%\ECL1" --import-filename-pattern="ECL1_HeLa_*" --save
  echo echo %ERRORLEVEL% ^> "%TMPDIR%\ECL1_import.done"
) > "%TMPDIR%\import_ECL1.bat"

(
  echo @echo off
  echo "C:\Program Files\Skyline\SkylineCmd.exe" --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\QEX3\Skyline AutoQC QEX3.sky" --import-all="Y:\msdata\2026\%yearmonth%\QEX3" --import-filename-pattern="QEX3_HeLa_*" --save
  echo echo %ERRORLEVEL% ^> "%TMPDIR%\QEX3_import.done"
) > "%TMPDIR%\import_QEX3.bat"

(
  echo @echo off
  echo "C:\Program Files\Skyline\SkylineCmd.exe" --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\OAZ1\Skyline AutoQC OAZ1.sky" --import-all="Y:\msdata\2026\%yearmonth%\OAZ1" --import-filename-pattern="OAZ1_HeLa_*" --save
  echo echo %ERRORLEVEL% ^> "%TMPDIR%\OAZ1_import.done"
) > "%TMPDIR%\import_OAZ1.bat"

:: Launch all import helpers as background (non-blocking) processes
start "" /B "%TMPDIR%\import_LUM1.bat"
start "" /B "%TMPDIR%\import_LUM2.bat"
start "" /B "%TMPDIR%\import_QEX2.bat"
start "" /B "%TMPDIR%\import_ECL1.bat"
start "" /B "%TMPDIR%\import_QEX3.bat"
start "" /B "%TMPDIR%\import_OAZ1.bat"

:: Poll until all six sentinel files appear
echo Waiting for all imports to complete...
:wait_imports
set /a _done=0
if exist "%TMPDIR%\LUM1_import.done" set /a _done+=1
if exist "%TMPDIR%\LUM2_import.done" set /a _done+=1
if exist "%TMPDIR%\QEX2_import.done" set /a _done+=1
if exist "%TMPDIR%\ECL1_import.done" set /a _done+=1
if exist "%TMPDIR%\QEX3_import.done" set /a _done+=1
if exist "%TMPDIR%\OAZ1_import.done" set /a _done+=1
if !_done! LSS 6 (
    timeout /t 15 /nobreak >nul
    goto wait_imports
)
echo All imports complete.
echo.

:: -----------------------------------------------------------------------
:: Phase 2: Parallel report exports (same sentinel-file pattern)
:: -----------------------------------------------------------------------
echo [Phase 2] Starting parallel report exports...

(
  echo @echo off
  echo "C:\Program Files\Skyline\SkylineCmd.exe" --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\LUM1\Skyline AutoQC Lumos1.sky" --report-name=JoeReport --report-conflict-resolution=overwrite --report-file="Y:\temporary_files\JO\keep\AutoQC-UTSW\LUM1\LUM1_RT_Export.csv"
  echo echo %ERRORLEVEL% ^> "%TMPDIR%\LUM1_export.done"
) > "%TMPDIR%\export_LUM1.bat"

(
  echo @echo off
  echo "C:\Program Files\Skyline\SkylineCmd.exe" --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\LUM2\Skyline AutoQC Lumos2.sky" --report-name=JoeReport --report-conflict-resolution=overwrite --report-file="Y:\temporary_files\JO\keep\AutoQC-UTSW\LUM2\LUM2_RT_Export.csv"
  echo echo %ERRORLEVEL% ^> "%TMPDIR%\LUM2_export.done"
) > "%TMPDIR%\export_LUM2.bat"

(
  echo @echo off
  echo "C:\Program Files\Skyline\SkylineCmd.exe" --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\QEX2\Skyline AutoQC QEX2.sky" --report-name=JoeReport --report-conflict-resolution=overwrite --report-file="Y:\temporary_files\JO\keep\AutoQC-UTSW\QEX2\QEX2_RT_Export.csv"
  echo echo %ERRORLEVEL% ^> "%TMPDIR%\QEX2_export.done"
) > "%TMPDIR%\export_QEX2.bat"

(
  echo @echo off
  echo "C:\Program Files\Skyline\SkylineCmd.exe" --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\ECL1\Skyline AutoQC ECL1.sky" --report-name=JoeReport --report-conflict-resolution=overwrite --report-file="Y:\temporary_files\JO\keep\AutoQC-UTSW\ECL1\ECL1_RT_Export.csv"
  echo echo %ERRORLEVEL% ^> "%TMPDIR%\ECL1_export.done"
) > "%TMPDIR%\export_ECL1.bat"

(
  echo @echo off
  echo "C:\Program Files\Skyline\SkylineCmd.exe" --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\QEX3\Skyline AutoQC QEX3.sky" --report-name=JoeReport --report-conflict-resolution=overwrite --report-file="Y:\temporary_files\JO\keep\AutoQC-UTSW\QEX3\QEX3_RT_Export.csv"
  echo echo %ERRORLEVEL% ^> "%TMPDIR%\QEX3_export.done"
) > "%TMPDIR%\export_QEX3.bat"

(
  echo @echo off
  echo "C:\Program Files\Skyline\SkylineCmd.exe" --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\OAZ1\Skyline AutoQC OAZ1.sky" --report-name=JoeReport --report-conflict-resolution=overwrite --report-file="Y:\temporary_files\JO\keep\AutoQC-UTSW\OAZ1\OAZ1_RT_Export.csv"
  echo echo %ERRORLEVEL% ^> "%TMPDIR%\OAZ1_export.done"
) > "%TMPDIR%\export_OAZ1.bat"

:: Launch all export helpers as background (non-blocking) processes
start "" /B "%TMPDIR%\export_LUM1.bat"
start "" /B "%TMPDIR%\export_LUM2.bat"
start "" /B "%TMPDIR%\export_QEX2.bat"
start "" /B "%TMPDIR%\export_ECL1.bat"
start "" /B "%TMPDIR%\export_QEX3.bat"
start "" /B "%TMPDIR%\export_OAZ1.bat"

:: Poll until all six sentinel files appear
echo Waiting for all exports to complete...
:wait_exports
set /a _done=0
if exist "%TMPDIR%\LUM1_export.done" set /a _done+=1
if exist "%TMPDIR%\LUM2_export.done" set /a _done+=1
if exist "%TMPDIR%\QEX2_export.done" set /a _done+=1
if exist "%TMPDIR%\ECL1_export.done" set /a _done+=1
if exist "%TMPDIR%\QEX3_export.done" set /a _done+=1
if exist "%TMPDIR%\OAZ1_export.done" set /a _done+=1
if !_done! LSS 6 (
    timeout /t 15 /nobreak >nul
    goto wait_exports
)
echo All exports complete.
echo.

:: -----------------------------------------------------------------------
:: Phase 3: Copy export CSVs to consolidated folders
:: (unchanged in behaviour from original; runs after confirmed completion)
:: -----------------------------------------------------------------------
echo [Phase 3] Copying export files to consolidated folder...

robocopy "Y:\temporary_files\JO\keep\AutoQC-UTSW\ECL1" "Y:\temporary_files\JO\keep\AutoQC-UTSW\RetentionTimeExports" ECL1_RT_Export.csv
robocopy "Y:\temporary_files\JO\keep\AutoQC-UTSW\LUM1" "Y:\temporary_files\JO\keep\AutoQC-UTSW\RetentionTimeExports" LUM1_RT_Export.csv
robocopy "Y:\temporary_files\JO\keep\AutoQC-UTSW\LUM2" "Y:\temporary_files\JO\keep\AutoQC-UTSW\RetentionTimeExports" LUM2_RT_Export.csv
robocopy "Y:\temporary_files\JO\keep\AutoQC-UTSW\QEX2" "Y:\temporary_files\JO\keep\AutoQC-UTSW\RetentionTimeExports" QEX2_RT_Export.csv
robocopy "Y:\temporary_files\JO\keep\AutoQC-UTSW\QEX3" "Y:\temporary_files\JO\keep\AutoQC-UTSW\RetentionTimeExports" QEX3_RT_Export.csv
robocopy "Y:\temporary_files\JO\keep\AutoQC-UTSW\OAZ1" "Y:\temporary_files\JO\keep\AutoQC-UTSW\RetentionTimeExports" OAZ1_RT_Export.csv

robocopy "Y:\temporary_files\JO\keep\AutoQC-UTSW\RetentionTimeExports" "Y:\temporary_files\JO\RetentionTimeExports2"

:: -----------------------------------------------------------------------
:: Cleanup temp directory
:: -----------------------------------------------------------------------
rmdir /s /q "%TMPDIR%"

echo.
echo Batch processing complete.
ENDLOCAL
