; Automation Hub Installer Script
; This script creates a Windows installer using Inno Setup
; Download Inno Setup from: https://jrsoftware.org/isdl.php

#define MyAppName "Automation Hub"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "JO Lab"
#define MyAppExeName "AutomationHub.App.exe"
#define MyAppAssocName MyAppName + " File"
#define MyAppAssocExt ".ahub"
#define MyAppAssocKey StringChange(MyAppAssocName, " ", "") + MyAppAssocExt

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
AppId={{A5B8C9D0-E1F2-4A3B-9C8D-7E6F5A4B3C2D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=
InfoBeforeFile=
InfoAfterFile=
; Uncomment the following line to run in non administrative install mode (install for current user only.)
;PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=installer-output
OutputBaseFilename=AutomationHub-Setup-{#MyAppVersion}
SetupIconFile=
Compression=lzma
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "publish\win-x64\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Sample configuration files
Source: "config\*"; DestDir: "{app}\config"; Flags: ignoreversion recursesubdirs createallsubdirs
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNetDesktopRuntimeInstalled: Boolean;
var
  ResultCode: Integer;
  Output: AnsiString;
begin
  // Check if .NET 8 Desktop Runtime is installed
  if Exec('cmd.exe', '/c dotnet --list-runtimes | findstr "Microsoft.WindowsDesktop.App 8."', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    Result := (ResultCode = 0);
  end
  else
  begin
    Result := False;
  end;
end;

function InitializeSetup: Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  
  if not IsDotNetDesktopRuntimeInstalled then
  begin
    if MsgBox('.NET 8 Desktop Runtime is required but not installed.' + #13#10 + #13#10 +
              'Would you like to open the download page?' + #13#10 +
              'After installing .NET 8 Desktop Runtime, please run this installer again.', 
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0', '', '', SW_SHOW, ewNoWait, ResultCode);
    end;
    Result := False;
  end;
end;
