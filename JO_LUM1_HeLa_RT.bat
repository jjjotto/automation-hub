cd C:\Users\s131945\AppData\Local\Apps\2.0\QQ4WYVGA.7BT\Q8666ON1.9TJ\skyl..tion_2e441fc3bf6adc7f_0017.0001_f0a1d88b2514a5aa

set yyyy=%date:~10,5%
set mm=%date:~4,2%
%yyyy%_%mm%

SkylineCmd.exe --in="Y:\temporary_files\JO\bckup_proc_pc\E\AutoQC-UTSW\LUM1\Skyline AutoQC Lumos1.sky" --import-all="Y:\msdata\2026\%yyyy%_%mm%\LUM1" --import-filename-pattern="LUM1_HeLa_*" --save

SkylineCmd.exe --in="Y:\temporary_files\JO\bckup_proc_pc\E\AutoQC-UTSW\LUM1\Skyline AutoQC Lumos1.sky" --report-name=JoeReport --report-conflict-resolution=overwrite --report-file="Y:\temporary_files\JO\bckup_proc_pc\E\AutoQC-UTSW\LUM1\LUM1_RT_Export.csv"

robocopy "Y:\temporary_files\JO\bckup_proc_pc\E\AutoQC-UTSW\LUM1" "Y:\temporary_files\JO\bckup_proc_pc\E\AutoQC-UTSW\RetentionTimeExports" LUM1_RT_Export.csv

robocopy "Y:\temporary_files\JO\bckup_proc_pc\E\AutoQC-UTSW\RetentionTimeExports" "Y:\temporary_files\JO\RetentionTimeExports2"