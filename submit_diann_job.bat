@echo off
setlocal

rem ==== run like this: submit_diann_job.bat "Y:\msdata\2026\2026_04\OAZ1\OAZ1_HeLa_20260409_E0000532676-20_50ng.raw"

rem ===== user/configurable values =====
set "API_BASE=http://129.112.164.124:9999"
set "RAWFILE=%~1"
set "STUDY=Automatic_HeLa"
set "ORGANISM=HUMAN_reviewed_9606_2025_12_31"

rem Optional: leave blank to allow any worker to pick it up
set "TARGET_HOST="

if "%RAWFILE%"=="" (
    echo Usage: submit_diann_job.bat ^<full_path_to_raw_file^>
    exit /b 1
)

for %%I in ("%RAWFILE%") do (
    set "JOBNAME=%%~nI"
)

if "%TARGET_HOST%"=="" (
    powershell -NoProfile -Command ^
      "$body = @{ job_name='%JOBNAME%'; organism='%ORGANISM%'; option1=$true; option2=$false; peptide_postprocess=$false; raw_files=@('%RAWFILE%'); study_folder='%STUDY%' } | ConvertTo-Json -Depth 4; " ^
      "Invoke-RestMethod -Uri '%API_BASE%/jobs' -Method Post -ContentType 'application/json' -Body $body | ConvertTo-Json -Depth 6"
) else (
    powershell -NoProfile -Command ^
      "$body = @{ job_name='%JOBNAME%'; organism='%ORGANISM%'; option1=$true; option2=$false; peptide_postprocess=$false; raw_files=@('%RAWFILE%'); study_folder='%STUDY%'; target_host='%TARGET_HOST%' } | ConvertTo-Json -Depth 4; " ^
      "Invoke-RestMethod -Uri '%API_BASE%/jobs' -Method Post -ContentType 'application/json' -Body $body | ConvertTo-Json -Depth 6"
)

endlocal