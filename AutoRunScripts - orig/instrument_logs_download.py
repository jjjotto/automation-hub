import requests
import os
import time
import pandas as pd # Import pandas
import io          # Import io to handle bytes data with pandas

# --- Configuration ---
# Use a raw string (r"...") for Windows paths to avoid issues with backslashes
OUTPUT_DIR = r"Y:\temporary_files\JO\keep\AutoRunScripts\outputs\instrumentlogs"

# Dictionary mapping the desired filename (without extension) to the Google Sheet URL
SHEETS_TO_DOWNLOAD = {
    "QEX2": "https://docs.google.com/spreadsheets/d/1mpPvBH5P8vAi-N7wixaz3PG5s74HoXfhTaKNwd8cp8E/edit?gid=1880636121#gid=1880636121",
    "QEX3": "https://docs.google.com/spreadsheets/d/1mpPvBH5P8vAi-N7wixaz3PG5s74HoXfhTaKNwd8cp8E/edit?gid=102757263#gid=102757263",
    "LUM1": "https://docs.google.com/spreadsheets/d/1mpPvBH5P8vAi-N7wixaz3PG5s74HoXfhTaKNwd8cp8E/edit?gid=547809416#gid=547809416",
    "LUM2": "https://docs.google.com/spreadsheets/d/1mpPvBH5P8vAi-N7wixaz3PG5s74HoXfhTaKNwd8cp8E/edit?gid=242301947#gid=242301947",
    "ECL1": "https://docs.google.com/spreadsheets/d/1mpPvBH5P8vAi-N7wixaz3PG5s74HoXfhTaKNwd8cp8E/edit?gid=1321565754#gid=1321565754",
}

# --- Helper Function for URL Transformation ---
def create_export_url(edit_url):
    """Transforms a Google Sheet edit URL into a CSV export URL."""
    try:
        base_part = edit_url.split('/edit?')[0]
        gid = edit_url.split('gid=')[-1]
        export_url = f"{base_part}/export?format=csv&gid={gid}"
        return export_url
    except IndexError:
        print(f"  Error: Could not parse GID from URL: {edit_url}")
        return None
    except Exception as e:
        print(f"  Error creating export URL for {edit_url}: {e}")
        return None

# --- Main Script Logic ---
print(f"Starting download process. Output directory: {OUTPUT_DIR}")

# Create the output directory if it doesn't exist
try:
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    print(f"Ensured output directory exists: {OUTPUT_DIR}")
except OSError as e:
    print(f"Error: Could not create directory {OUTPUT_DIR}. Please check permissions. Details: {e}")
    exit(1)

# Loop through each sheet defined in the dictionary
for instrument_name, edit_url in SHEETS_TO_DOWNLOAD.items():
    print(f"\nProcessing: {instrument_name}")

    export_url = create_export_url(edit_url)
    if not export_url:
        print(f"  Skipping {instrument_name} due to URL parsing error.")
        continue

    print(f"  Export URL: {export_url}")

    output_filename = f"{instrument_name}.csv"
    output_filepath = os.path.join(OUTPUT_DIR, output_filename)
    # Add XLSX output path
    output_xlsx_filename = f"{instrument_name}.xlsx"
    output_xlsx_filepath = os.path.join(OUTPUT_DIR, output_xlsx_filename)
    print(f"  Output file: {output_filepath}")

    try:
        # Download the data
        response = requests.get(export_url, timeout=60)
        response.raise_for_status()

        # Read the newly downloaded data into a pandas DataFrame
        # Use io.BytesIO because response.content is bytes
        try:
            # Use on_bad_lines='skip' or 'warn' if you expect potential CSV format issues
            new_data_df = pd.read_csv(io.BytesIO(response.content), on_bad_lines='warn')
            if new_data_df.empty:
                 print(f"  Warning: Downloaded data for {instrument_name} is empty. Skipping file update.")
                 continue # Skip if the downloaded sheet is empty
        except pd.errors.ParserError as e:
            print(f"  Error: Could not parse the downloaded CSV data for {instrument_name}. Skipping. Details: {e}")
            continue
        except Exception as e: # Catch other potential pandas errors
             print(f"  Error: An error occurred reading downloaded data for {instrument_name} into DataFrame. Skipping. Details: {e}")
             continue

        # Check if the output file already exists
        if os.path.exists(output_filepath) and os.path.getsize(output_filepath) > 0:
            print(f"  File exists. Attempting to append data.")
            try:
                # Read existing data
                existing_data_df = pd.read_csv(output_filepath, on_bad_lines='warn')

                # Check if columns match (order doesn't strictly matter, just presence and name)
                if list(existing_data_df.columns) == list(new_data_df.columns):
                    # Concatenate old and new data
                    combined_df = pd.concat([existing_data_df, new_data_df], ignore_index=True)
                    # Drop duplicate rows, keeping the last occurrence
                    rows_before_dedup = len(combined_df)
                    combined_df = combined_df.drop_duplicates(keep='last')
                    rows_after_dedup = len(combined_df)
                    print(f"  Combined data. Removed {rows_before_dedup - rows_after_dedup} duplicate rows.")
                    # Save the combined data, overwriting the old file
                    combined_df.to_csv(output_filepath, index=False)
                    # Save as XLSX as well
                    combined_df.to_excel(output_xlsx_filepath, index=False)
                    print(f"  Successfully appended and saved {output_filename} and {output_xlsx_filename}")
                else:
                    # Headers don't match - overwrite with new data as a safety measure
                    print(f"  Warning: Headers in existing file do not match downloaded data for {instrument_name}. Overwriting file with new data.")
                    new_data_df.to_csv(output_filepath, index=False)
                    new_data_df.to_excel(output_xlsx_filepath, index=False)
                    print(f"  Successfully overwrote {output_filename} and {output_xlsx_filename} with new data due to header mismatch.")

            except pd.errors.EmptyDataError:
                 print(f"  Warning: Existing file {output_filename} is empty. Overwriting with new data.")
                 new_data_df.to_csv(output_filepath, index=False)
                 new_data_df.to_excel(output_xlsx_filepath, index=False)
                 print(f"  Successfully saved {output_filename} and {output_xlsx_filename}")
            except pd.errors.ParserError as e:
                 print(f"  Error: Could not parse existing file {output_filename}. Overwriting with new data. Details: {e}")
                 new_data_df.to_csv(output_filepath, index=False)
                 new_data_df.to_excel(output_xlsx_filepath, index=False)
                 print(f"  Successfully overwrote {output_filename} and {output_xlsx_filename} due to parsing error in existing file.")
            except Exception as e:
                 print(f"  Error processing existing file {output_filename}. Skipping update for this file. Details: {e}")
                 # Optionally, you could still choose to overwrite here if preferred
                 # new_data_df.to_csv(output_filepath, index=False)
                 # new_data_df.to_excel(output_xlsx_filepath, index=False)
                 # print(f"  Overwrote {output_filename} and {output_xlsx_filename} due to error processing existing file.")

        else:
            # File doesn't exist or is empty, just write the new data
            print(f"  File does not exist or is empty. Writing new data.")
            new_data_df.to_csv(output_filepath, index=False)
            new_data_df.to_excel(output_xlsx_filepath, index=False)
            print(f"  Successfully saved new file {output_filename} and {output_xlsx_filename}")

    # Handle potential errors during the download or file writing
    except requests.exceptions.Timeout:
         print(f"  Error: The request timed out while downloading {instrument_name}.")
    except requests.exceptions.HTTPError as e:
         print(f"  Error: HTTP error occurred for {instrument_name}: {e.response.status_code} {e.response.reason}")
         print(f"  Check if the URL is correct and the sheet is publicly accessible.")
    except requests.exceptions.RequestException as e:
        print(f"  Error: Failed to download {instrument_name}. Details: {e}")
    except IOError as e: # Catch errors during pandas write operation
        print(f"  Error: Could not write file {output_filepath}. Details: {e}")
    except Exception as e:
        print(f"  An unexpected error occurred for {instrument_name}: {e}")
        import traceback
        traceback.print_exc() # Print traceback for unexpected errors

    time.sleep(1)

print("\nDownload process finished.")
