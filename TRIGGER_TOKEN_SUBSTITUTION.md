# Trigger Token Substitution Guide

## Overview

When you create a file-triggered task, you can use special tokens to automatically insert the path to the detected file into your task's arguments. This guide explains how to properly configure this.

## Token Format

The following token formats are supported in the **Command**, **Arguments**, and **Working Directory** fields:

### Percent Format (Recommended for Batch Files)
- `%TRIGGER_FILE_PATH%` - Full path to the detected file
- `%TRIGGER_FILE_NAME%` - Just the filename without directory
- `%TRIGGER_FILE_DIRECTORY%` - Just the directory path
- Flexible spacing/underscores: `%TRIGGER FILE PATH%`, `%TRIGGER_FILE_PATH%`, etc.

### Brace Format (Alternative)
- `{TRIGGER_FILE_PATH}`
- `{TRIGGER FILE PATH}`
- etc.

## Example: submit_diann_job.bat Configuration

If you want to run `submit_diann_job.bat` with detected files, configure your task like this:

### Option 1: Arguments Field (Recommended)
- **Command:** `C:\WebApps\automation-hub\AutoRunScripts\submit_diann_job.bat`
- **Arguments:** `%TRIGGER_FILE_PATH%`
- **Working Directory:** (leave empty or set as needed)

When a file like `Y:\msdata\2026\2026_04\OAZ1\OAZ1_HeLa_20260409_E0000532676-20_50ng.raw` is detected, the system will:
1. Replace `%TRIGGER_FILE_PATH%` with the actual file path
2. Pass it as: `submit_diann_job.bat "Y:\msdata\2026\2026_04\OAZ1\OAZ1_HeLa_20260409_E0000532676-20_50ng.raw"`

### Option 2: Quoted Arguments
If your batch file expects quoted arguments:
- **Arguments:** `"%TRIGGER_FILE_PATH%"`

After substitution becomes: `"Y:\msdata\2026\2026_04\OAZ1\OAZ1_HeLa_20260409_E0000532676-20_50ng.raw"`

### Option 3: Multiple Arguments
- **Arguments:** `%TRIGGER_FILE_PATH% --log-output %TRIGGER_FILE_DIRECTORY%\logs`

## How Token Replacement Works

1. When a file is detected that matches your filters, the task system:
   - Captures the full file path
   - Replaces all token placeholders with the actual values
   - Passes the substituted command/arguments to the batch file

2. The system also sets environment variables that the batch file can use:
   - `TRIGGER_FILE_PATH` - Full path to the file
   - `TRIGGER_FILE_NAME` - Just the filename
   - `TRIGGER_FILE_DIRECTORY` - Just the directory
   - `TRIGGER FILE PATH` - Alternative format for the full path

3. This means your batch file can either:
   - Accept the path as an argument (via %1, %2, etc. in batch)
   - Or access the environment variables directly

## Debugging Token Substitution

If you're seeing `%TRIGGER_FILE_PATH%` in error messages instead of the actual file path:

1. **Check the Arguments field:** Verify it contains `%TRIGGER_FILE_PATH%` (not some other format)
2. **Check the Output Log:** Look for "Arguments substitution" messages showing what was replaced
3. **Ensure quotes are correct:** 
   - If you need spaces/special chars handled: use quotes like `"%TRIGGER_FILE_PATH%"`
   - Without quotes, the system auto-quotes arguments for batch files
4. **Verify batch file format:** Make sure the .bat file extension matches the command path

## File Readiness Check

Before the task executes, the system waits for the file to be stable (not being copied). This prevents processing incomplete files. You can control this behavior through file trigger settings.

## Example from Documentation

You can manually run your batch file like:
```
submit_diann_job.bat "Y:\msdata\2026\2026_04\OAZ1\OAZ1_HeLa_20260409_E0000532676-20_50ng.raw"
```

In the UI, you would configure this as a file-triggered task, and the system will automatically handle all the quoting and substitution when files are detected.
