# Automation Hub - User Interface Screenshots and Mockups

## Application Window Screenshots

Since this is a WPF application that requires Windows to run, this document provides detailed mockups and descriptions of the user interface based on the XAML design.

## Main Window - Default View

### Full Application Window

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║ Automation Hub                                           Minimize □ Maximize ✕ ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║                                                                               ║
║  Automation Hub Jobs                                                          ║
║                                                                               ║
║  ┏━━━━━━━━━━━━━━━━━━━━┳━━━━━━━━┳━━━━━━━━━┳━━━━━━━━━━━━━━━━━━┳━━━━━━━━━━━━━┓  ║
║  ┃ Name               ┃ Type   ┃ Enabled ┃ Command          ┃ Trigger Path┃  ║
║  ┣━━━━━━━━━━━━━━━━━━━━╋━━━━━━━━╋━━━━━━━━━╋━━━━━━━━━━━━━━━━━━╋━━━━━━━━━━━━━┫  ║
║  ┃ Sample ECL1 QC     ┃ Hybrid ┃ ☑       ┃ Y:/temporary_fi..┃ Y:/tempor...┃  ║
║  ┃ Export             ┃        ┃         ┃ /Batch_ImportN.. ┃ /ECL1       ┃  ║
║  ┃                    ┃        ┃         ┃                  ┃             ┃  ║
║  ┃                    ┃        ┃         ┃                  ┃             ┃  ║
║  ┃                    ┃        ┃         ┃                  ┃             ┃  ║
║  ┃                    ┃        ┃         ┃                  ┃             ┃  ║
║  ┃                    ┃        ┃         ┃                  ┃             ┃  ║
║  ┗━━━━━━━━━━━━━━━━━━━━┻━━━━━━━━┻━━━━━━━━━┻━━━━━━━━━━━━━━━━━━┻━━━━━━━━━━━━━┛  ║
║                                                                               ║
║  Loaded 1 job(s)                                                             ║
║                                                                               ║
╚═══════════════════════════════════════════════════════════════════════════════╝
```

**Dimensions:** 960px wide × 450px high

### UI Element Details

#### 1. Window Chrome
- **Title Bar**: Standard Windows title bar with application name "Automation Hub"
- **System Buttons**: Minimize, Maximize/Restore, Close
- **Background**: White/Light gray (standard WPF window background)

#### 2. Header Section
- **Text**: "Automation Hub Jobs"
- **Font**: Bold, 20pt
- **Color**: Black (default)
- **Position**: Top of window, left-aligned with 12px margin

#### 3. Data Grid (Main Content)
The DataGrid occupies most of the window space and displays job information in columns:

**Column Configuration:**
- **Name** (2x relative width)
  - Contains the job's display name
  - Example: "Sample ECL1 QC Export"
  - Left-aligned text

- **Type** (1x relative width)
  - Shows the JobType enum value
  - Values: Manual, Scheduled, FileTrigger, Hybrid, ManualAndFile, ManualAndScheduled
  - Example: "Hybrid"
  - Left-aligned text

- **Enabled** (Auto width)
  - Checkbox column (CheckBoxColumn)
  - Shows ☑ (checked) or ☐ (unchecked)
  - Interactive - can toggle job enabled state
  - Center-aligned

- **Command** (2x relative width)
  - Full path to the executable or script
  - Example: "Y:/temporary_files/JO/automation/AutoQC-UTSW/Batch_ImportNew_ExportRT.bat"
  - Text may be truncated with ellipsis if too long
  - Left-aligned text

- **Trigger Path** (2x relative width)
  - File system path being monitored (for FileTrigger jobs)
  - Example: "Y:/temporary_files/JO/automation/ECL1"
  - May be empty for Manual-only or Scheduled-only jobs
  - Left-aligned text

**Data Grid Features:**
- Column headers are always visible
- Alternating row backgrounds (standard DataGrid styling)
- Rows can be selected (highlighted)
- Horizontal scrollbar appears if content exceeds width
- Vertical scrollbar appears if jobs exceed visible area
- 12px margin on all sides

#### 4. Status Bar
- **Position**: Bottom of window
- **Font**: Italic, gray text
- **Color**: #808080 (Gray)
- **Margin**: 12px left/right/bottom
- **Content**: Dynamic status messages
  - "Ready" - Initial state
  - "Loaded 1 job(s)" - After successful load
  - "No jobs defined" - No JSON files found
  - "Jobs directory not found: [path]" - Directory doesn't exist
  - "Error loading jobs: [error message]" - Exception occurred

## State Examples

### Example 1: Application with Multiple Jobs

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║ Automation Hub                                           Minimize □ Maximize ✕ ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║                                                                               ║
║  Automation Hub Jobs                                                          ║
║                                                                               ║
║  ┏━━━━━━━━━━━━━━━━━━━━┳━━━━━━━━━━━━┳━━━━━━━━━┳━━━━━━━━━━━━━━┳━━━━━━━━━━━━━┓  ║
║  ┃ Name               ┃ Type       ┃ Enabled ┃ Command      ┃ Trigger Path┃  ║
║  ┣━━━━━━━━━━━━━━━━━━━━╋━━━━━━━━━━━━╋━━━━━━━━━╋━━━━━━━━━━━━━━╋━━━━━━━━━━━━━┫  ║
║  ┃ Sample ECL1 QC     ┃ Hybrid     ┃ ☑       ┃ Y:/temp.../..┃ Y:/temp/ECL1┃  ║
║  ┃ Export             ┃            ┃         ┃              ┃             ┃  ║
║  ┣━━━━━━━━━━━━━━━━━━━━╋━━━━━━━━━━━━╋━━━━━━━━━╋━━━━━━━━━━━━━━╋━━━━━━━━━━━━━┫  ║
║  ┃ Daily Backup       ┃ Scheduled  ┃ ☑       ┃ C:/backup.ps1┃             ┃  ║
║  ┣━━━━━━━━━━━━━━━━━━━━╋━━━━━━━━━━━━╋━━━━━━━━━╋━━━━━━━━━━━━━━╋━━━━━━━━━━━━━┫  ║
║  ┃ Manual Data Export ┃ Manual     ┃ ☐       ┃ export.bat   ┃             ┃  ║
║  ┣━━━━━━━━━━━━━━━━━━━━╋━━━━━━━━━━━━╋━━━━━━━━━╋━━━━━━━━━━━━━━╋━━━━━━━━━━━━━┫  ║
║  ┃ File Watcher Job   ┃ FileTrigger┃ ☑       ┃ process.py   ┃ C:/data/in  ┃  ║
║  ┗━━━━━━━━━━━━━━━━━━━━┻━━━━━━━━━━━━┻━━━━━━━━━┻━━━━━━━━━━━━━━┻━━━━━━━━━━━━━┛  ║
║                                                                               ║
║  Loaded 4 job(s)                                                             ║
║                                                                               ║
╚═══════════════════════════════════════════════════════════════════════════════╝
```

### Example 2: No Jobs Defined

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║ Automation Hub                                           Minimize □ Maximize ✕ ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║                                                                               ║
║  Automation Hub Jobs                                                          ║
║                                                                               ║
║  ┏━━━━━━━━━━━━━━━━━━━━┳━━━━━━━━┳━━━━━━━━━┳━━━━━━━━━━━━━━━━━━┳━━━━━━━━━━━━━┓  ║
║  ┃ Name               ┃ Type   ┃ Enabled ┃ Command          ┃ Trigger Path┃  ║
║  ┣━━━━━━━━━━━━━━━━━━━━╋━━━━━━━━╋━━━━━━━━━╋━━━━━━━━━━━━━━━━━━╋━━━━━━━━━━━━━┫  ║
║  ┃                    ┃        ┃         ┃                  ┃             ┃  ║
║  ┃                    ┃        ┃         ┃                  ┃             ┃  ║
║  ┃          (No jobs to display)         ┃                  ┃             ┃  ║
║  ┃                    ┃        ┃         ┃                  ┃             ┃  ║
║  ┃                    ┃        ┃         ┃                  ┃             ┃  ║
║  ┗━━━━━━━━━━━━━━━━━━━━┻━━━━━━━━┻━━━━━━━━━┻━━━━━━━━━━━━━━━━━━┻━━━━━━━━━━━━━┛  ║
║                                                                               ║
║  No jobs defined                                                             ║
║                                                                               ║
╚═══════════════════════════════════════════════════════════════════════════════╝
```

### Example 3: Directory Not Found Error

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║ Automation Hub                                           Minimize □ Maximize ✕ ║
╠═══════════════════════════════════════════════════════════════════════════════╣
║                                                                               ║
║  Automation Hub Jobs                                                          ║
║                                                                               ║
║  ┏━━━━━━━━━━━━━━━━━━━━┳━━━━━━━━┳━━━━━━━━━┳━━━━━━━━━━━━━━━━━━┳━━━━━━━━━━━━━┓  ║
║  ┃ Name               ┃ Type   ┃ Enabled ┃ Command          ┃ Trigger Path┃  ║
║  ┣━━━━━━━━━━━━━━━━━━━━╋━━━━━━━━╋━━━━━━━━━╋━━━━━━━━━━━━━━━━━━╋━━━━━━━━━━━━━┫  ║
║  ┃                    ┃        ┃         ┃                  ┃             ┃  ║
║  ┃                    ┃        ┃         ┃                  ┃             ┃  ║
║  ┃          (No jobs to display)         ┃                  ┃             ┃  ║
║  ┃                    ┃        ┃         ┃                  ┃             ┃  ║
║  ┃                    ┃        ┃         ┃                  ┃             ┃  ║
║  ┗━━━━━━━━━━━━━━━━━━━━┻━━━━━━━━┻━━━━━━━━━┻━━━━━━━━━━━━━━━━━━┻━━━━━━━━━━━━━┛  ║
║                                                                               ║
║  Jobs directory not found: Y:\temporary_files\JO\automation\config\jobs      ║
║                                                                               ║
╚═══════════════════════════════════════════════════════════════════════════════╝
```

## Design Specifications

### Color Scheme
- **Background**: White (#FFFFFF) or default system window color
- **Primary Text**: Black (#000000)
- **Header Text**: Black (#000000), Bold
- **Status Text**: Gray (#808080), Italic
- **Grid Headers**: Light gray background with dark text (standard WPF DataGrid)
- **Grid Borders**: Light gray (#D3D3D3)
- **Selected Row**: System accent color (typically blue highlight)
- **Checkbox Checked**: System accent color or blue
- **Checkbox Unchecked**: Gray border

### Typography
- **Header Font**: Segoe UI, 20pt, Bold
- **Grid Content**: Segoe UI, 11pt, Regular
- **Status Bar**: Segoe UI, 11pt, Italic

### Spacing
- **Window Margin**: 12px on all sides
- **Grid Margin**: 0px top, 12px bottom and left/right
- **Row Height**: Auto (based on content)
- **Column Padding**: 4px (standard DataGrid)

### Interactive Elements
- **Checkboxes**: Standard WPF CheckBox controls in the Enabled column
- **Row Selection**: Single row selection, highlighted with system accent color
- **Hover Effects**: Standard WPF DataGrid hover behavior
- **Scrollbars**: Appear automatically when content exceeds visible area

## Data Binding

The UI uses WPF data binding to connect to the `MainWindowViewModel`:

```
Window.DataContext → MainWindowViewModel
  ├─ Jobs (ObservableCollection<JobDefinition>)
  │   ├─ JobDefinition.Name → Name column
  │   ├─ JobDefinition.Type → Type column
  │   ├─ JobDefinition.Enabled → Enabled checkbox
  │   ├─ JobDefinition.Process.Command → Command column
  │   └─ JobDefinition.FileTrigger.WatchPath → Trigger Path column
  └─ StatusMessage (string) → Status bar text
```

## Future UI Enhancements

The current UI is a foundation for planned features:

1. **Control Buttons**: Add buttons for manual trigger, enable/disable all, refresh
2. **Job Details Panel**: Show expanded job details when a row is selected
3. **Status Indicators**: Real-time status indicators (running, idle, error)
4. **Log Viewer**: View job execution logs in a separate panel or window
5. **Context Menu**: Right-click menu for job-specific actions
6. **Toolbar**: Add toolbar with common actions
7. **Filter/Search**: Search and filter jobs by name, type, or status
8. **Job Editor**: In-app editor for creating/modifying jobs
9. **System Tray**: Minimize to system tray with notifications
10. **Dark Mode**: Support for dark theme

## Technical Notes

### XAML Structure
The UI is defined in `MainWindow.xaml` with:
- Grid layout with 3 rows (Auto, *, Auto)
- DataGrid with 5 columns
- Data binding expressions using `{Binding}` syntax
- No custom styling (uses default WPF theme)

### ViewModel Pattern
The application uses the MVVM pattern:
- `MainWindow.xaml.cs` - View (code-behind minimal)
- `MainWindowViewModel.cs` - ViewModel with INotifyPropertyChanged
- `JobDefinition.cs` - Model (data classes)

### Responsive Behavior
- Window is resizable
- DataGrid columns resize proportionally
- Scrollbars appear as needed
- Status message updates dynamically
