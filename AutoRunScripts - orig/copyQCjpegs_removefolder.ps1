# Define source and destination paths
$sourcePath = "Z:\"
$destinationBasePath = "Y:\temporary_files\JO\keep\AutoRunScripts\outputs\qc_images"

# Define the folder names
$folders = @("ECL1", "LUM1", "LUM2", "QEX2", "QEX3")

# Get the current date
$currentDate = Get-Date

# Function to copy JPEG files to the appropriate folder
function Copy-JpegFiles {
    param (
        [string]$sourcePath,
        [string]$destinationBasePath,
        [array]$folders
    )

    # Get all JPEG files recursively from the source path
    $jpegFiles = Get-ChildItem -Path $sourcePath -Recurse -Filter *.jpeg

    foreach ($file in $jpegFiles) {
        # Extract the folder name from the file name
        $folderName = $folders | Where-Object { $file.Name -match $_ }

        if ($folderName) {
            #$destinationPath = Join-Path -Path $destinationBasePath -ChildPath $folderName
            $destinationPath = Join-Path -Path $destinationBasePath -ChildPath ($folderName)


            # Ensure the destination folder exists
            if (-not (Test-Path -Path $destinationPath)) {
                New-Item -Path $destinationPath -ItemType Directory
            }

            # Define the destination file path
            $destinationFilePath = Join-Path -Path $destinationPath -ChildPath $file.Name

            # Copy the file if it doesn't already exist in the destination folder
            if (-not (Test-Path -Path $destinationFilePath)) {
                Copy-Item -Path $file.FullName -Destination $destinationFilePath
            }
        }
    }
}

# Function to remove folders older than 7 days
function Remove-OldFolders {
    param (
        [string]$sourcePath,
        [int]$daysOld
    )

    # Get all directories from the source path
    $directories = Get-ChildItem -Path $sourcePath -Directory

    foreach ($directory in $directories) {
        # Calculate the folder age
        $folderAge = ($currentDate - $directory.CreationTime).Days

        # Remove the folder if it is older than the specified number of days
        if ($folderAge -ge $daysOld) {
            Remove-Item -Path $directory.FullName -Recurse -Force
        }
    }
}

# Copy JPEG files to the appropriate folders
Copy-JpegFiles -sourcePath $sourcePath -destinationBasePath $destinationBasePath -folders $folders

# Remove folders older than 7 days
Remove-OldFolders -sourcePath $sourcePath -daysOld 8