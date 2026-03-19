::this is for Skyline version 23.1.0.268

::cd C:\Users\s131945\AppData\Local\Apps\2.0\QQ4WYVGA.7BT\Q8666ON1.9TJ\skyl..tion_2e441fc3bf6adc7f_0017.0001_f0a1d88b2514a5aa

cd "C:\Program Files\Skyline"

set yyyy=%date:~10,5%
set mm=%date:~4,2%
%yyyy%_%mm%

SkylineCmd.exe --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\LUM1\Skyline AutoQC Lumos1.sky" --import-all="Y:\msdata\2026\%yyyy%_%mm%\LUM1" --import-filename-pattern="LUM1_HeLa_*" --save
SkylineCmd.exe --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\LUM2\Skyline AutoQC Lumos2.sky" --import-all="Y:\msdata\2026\%yyyy%_%mm%\LUM2" --import-filename-pattern="LUM2_HeLa_*" --save
SkylineCmd.exe --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\QEX2\Skyline AutoQC QEX2.sky" --import-all="Y:\msdata\2026\%yyyy%_%mm%\QEX2" --import-filename-pattern="QEX2_HeLa_*" --save
SkylineCmd.exe --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\ECL1\Skyline AutoQC ECL1.sky" --import-all="Y:\msdata\2026\%yyyy%_%mm%\ECL1" --import-filename-pattern="ECL1_HeLa_*" --save
SkylineCmd.exe --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\QEX3\Skyline AutoQC QEX3.sky" --import-all="Y:\msdata\2026\%yyyy%_%mm%\QEX3" --import-filename-pattern="QEX3_HeLa_*" --save
SkylineCmd.exe --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\OAZ1\Skyline AutoQC OAZ1.sky" --import-all="Y:\msdata\2026\%yyyy%_%mm%\OAZ1" --import-filename-pattern="OAZ1_HeLa_*" --save
::SkylineCmd.exe --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\OTE1\Skyline AutoQC OTE1.sky" --import-all="Y:\msdata\2026\%yyyy%_%mm%\OTE1" --import-filename-pattern="OTE1_HeLa_*" --save

SkylineCmd.exe --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\LUM1\Skyline AutoQC Lumos1.sky" --report-name=JoeReport --report-conflict-resolution=overwrite --report-file="Y:\temporary_files\JO\keep\AutoQC-UTSW\LUM1\LUM1_RT_Export.csv"
SkylineCmd.exe --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\LUM2\Skyline AutoQC Lumos2.sky" --report-name=JoeReport --report-conflict-resolution=overwrite --report-file="Y:\temporary_files\JO\keep\AutoQC-UTSW\LUM2\LUM2_RT_Export.csv"
SkylineCmd.exe --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\QEX2\Skyline AutoQC QEX2.sky" --report-name=JoeReport --report-conflict-resolution=overwrite --report-file="Y:\temporary_files\JO\keep\AutoQC-UTSW\QEX2\QEX2_RT_Export.csv"
SkylineCmd.exe --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\ECL1\Skyline AutoQC ECL1.sky" --report-name=JoeReport --report-conflict-resolution=overwrite --report-file="Y:\temporary_files\JO\keep\AutoQC-UTSW\ECL1\ECL1_RT_Export.csv"
SkylineCmd.exe --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\QEX3\Skyline AutoQC QEX3.sky" --report-name=JoeReport --report-conflict-resolution=overwrite --report-file="Y:\temporary_files\JO\keep\AutoQC-UTSW\QEX3\QEX3_RT_Export.csv"
SkylineCmd.exe --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\OAZ1\Skyline AutoQC OAZ1.sky" --report-name=JoeReport --report-conflict-resolution=overwrite --report-file="Y:\temporary_files\JO\keep\AutoQC-UTSW\OAZ1\OAZ1_RT_Export.csv"
::SkylineCmd.exe --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\OTE1\Skyline AutoQC OTE1.sky" --report-name=JoeReport --report-conflict-resolution=overwrite --report-file="Y:\temporary_files\JO\keep\AutoQC-UTSW\OTE1\OTE1_RT_Export.csv"

::SkylineCmd.exe --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\LUM1\Skyline AutoQC Lumos1.sky" --report-name=JoeReport --report-conflict-resolution=overwrite --report-file="Y:\temporary_files\JO\RetentionTimeExports\LUM1_RT_Export.csv"
::SkylineCmd.exe --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\LUM2\Skyline AutoQC Lumos2.sky" --report-name=JoeReport --report-conflict-resolution=overwrite --report-file="Y:\temporary_files\JO\RetentionTimeExports\LUM2_RT_Export.csv"
::SkylineCmd.exe --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\QEX2\Skyline AutoQC QEX2.sky" --report-name=JoeReport --report-conflict-resolution=overwrite --report-file="Y:\temporary_files\JO\RetentionTimeExports\QEX2_RT_Export.csv"
::SkylineCmd.exe --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\ECL1\Skyline AutoQC ECL1.sky" --report-name=JoeReport --report-conflict-resolution=overwrite --report-file="Y:\temporary_files\JO\RetentionTimeExports\ECL1_RT_Export.csv"
::SkylineCmd.exe --in="Y:\temporary_files\JO\keep\AutoQC-UTSW\QEX3\Skyline AutoQC QEX3.sky" --report-name=JoeReport --report-conflict-resolution=overwrite --report-file="Y:\temporary_files\JO\RetentionTimeExports\QEX3_RT_Export.csv"

robocopy "Y:\temporary_files\JO\keep\AutoQC-UTSW\ECL1" "Y:\temporary_files\JO\keep\AutoQC-UTSW\RetentionTimeExports" ECL1_RT_Export.csv
robocopy "Y:\temporary_files\JO\keep\AutoQC-UTSW\LUM1" "Y:\temporary_files\JO\keep\AutoQC-UTSW\RetentionTimeExports" LUM1_RT_Export.csv
robocopy "Y:\temporary_files\JO\keep\AutoQC-UTSW\LUM2" "Y:\temporary_files\JO\keep\AutoQC-UTSW\RetentionTimeExports" LUM2_RT_Export.csv
robocopy "Y:\temporary_files\JO\keep\AutoQC-UTSW\QEX2" "Y:\temporary_files\JO\keep\AutoQC-UTSW\RetentionTimeExports" QEX2_RT_Export.csv
robocopy "Y:\temporary_files\JO\keep\AutoQC-UTSW\QEX3" "Y:\temporary_files\JO\keep\AutoQC-UTSW\RetentionTimeExports" QEX3_RT_Export.csv
robocopy "Y:\temporary_files\JO\keep\AutoQC-UTSW\OAZ1" "Y:\temporary_files\JO\keep\AutoQC-UTSW\RetentionTimeExports" OAZ1_RT_Export.csv

robocopy "Y:\temporary_files\JO\keep\AutoQC-UTSW\RetentionTimeExports" "Y:\temporary_files\JO\RetentionTimeExports2"
