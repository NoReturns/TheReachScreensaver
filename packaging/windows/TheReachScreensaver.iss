#define AppVersion "1.1.0"
#ifndef PackageRoot
  #define PackageRoot "..\..\artifacts\v1.1.0\TheReachScreensaver-1.1.0-win-x64"
#endif
#ifndef CandidateOutput
  #define CandidateOutput "..\..\artifacts\v1.1.0"
#endif
#ifndef CandidateName
  #define CandidateName "TheReachScreensaver-1.1.0-Setup"
#endif

[Setup]
AppId={{B98A81E3-3A1A-47B4-85FC-10AD5B48EF72}
AppName=The Reach Screensaver
AppVersion={#AppVersion}
AppVerName=The Reach Screensaver {#AppVersion}
AppPublisher=NoReturns
AppPublisherURL=https://github.com/NoReturns/TheReachScreensaver
DefaultDirName={localappdata}\Programs\TheReachScreensaver
UninstallFilesDir={localappdata}\Programs\TheReachScreensaver.Uninstall
UsePreviousAppDir=no
DisableDirPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
OutputDir={#CandidateOutput}
OutputBaseFilename={#CandidateName}
OutputManifestFile={#CandidateName}-Manifest.txt
Compression=lzma2
SolidCompression=yes
VersionInfoVersion=1.1.0.0
VersionInfoProductVersion=1.1.0.0
VersionInfoDescription=The Reach Screensaver installer
UninstallDisplayIcon={app}\TheReachScreensaver.exe
LicenseFile={#PackageRoot}\LICENSE
CloseApplications=no
RestartApplications=no
ChangesEnvironment=no
WizardStyle=modern

[Files]
Source: "{#PackageRoot}\*"; DestDir: "{localappdata}\Programs\TheReachScreensaver.pending-v1.0.0"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#PackageRoot}\windows\Prepare-ReachUpgrade.ps1"; Flags: dontcopy
Source: "{#PackageRoot}\windows\Complete-ReachUpgrade.ps1"; Flags: dontcopy

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Icons]
Name: "{userprograms}\The Reach Screensaver\Select The Reach Screensaver"; Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\windows\Select-ReachFromSetup.ps1"" -Shortcut"; WorkingDir: "{app}"; IconFilename: "{app}\TheReachScreensaver.exe"

[Run]
Filename: "{sys}\WindowsPowerShell\v1.0\powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\windows\Select-ReachFromSetup.ps1"""; Description: "Set The Reach as my Windows screensaver"; Flags: postinstall skipifsilent unchecked
Filename: "{app}\TheReachScreensaver.exe"; Parameters: "/c"; Description: "Open The Reach Screensaver Settings"; Flags: postinstall skipifsilent unchecked nowait

[Code]
var
  BackupCleanupRequested: Boolean;
  BackupCleanupScript: String;
  BackupCleanupRoot: String;

function ReadBackupCleanupReport(const ReportPath: String; var Status, Path, SizeMB, Detail: String): Boolean;
var
  Lines: TStringList;
begin
  Result := False;
  if not FileExists(ReportPath) then
    Exit;
  Lines := TStringList.Create;
  try
    try
      Lines.LoadFromFile(ReportPath);
      if Lines.Count < 4 then
        Exit;
      Status := Lines[0];
      Path := Lines[1];
      SizeMB := Lines[2];
      Detail := Lines[3];
      Result := True;
    except
      Result := False;
    end;
  finally
    Lines.Free;
  end;
end;

function RunBackupCleanup(const Action, ReportPath: String; var Status, Path, SizeMB, Detail: String): Boolean;
var
  ResultCode: Integer;
  Parameters: String;
begin
  Result := False;
  if FileExists(ReportPath) then
    DeleteFile(ReportPath);
  Detail := 'Backup inspection did not run. Backups will be kept.';
  Parameters := '-NoProfile -ExecutionPolicy Bypass -File "' + BackupCleanupScript +
    '" -Action ' + Action + ' -ReportFile "' + ReportPath +
    '" -LocalDataRoot "' + ExpandConstant('{localappdata}') + '"';
  if not Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
    Parameters, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
    Exit;
  if not ReadBackupCleanupReport(ReportPath, Status, Path, SizeMB, Detail) then
  begin
    Detail := 'Backup inspection returned no readable report. Backups will be kept.';
    Exit;
  end;
  Result := ResultCode = 0;
  if not Result then
    Detail := 'Backup inspection failed (exit code ' + IntToStr(ResultCode) + '): ' + Detail;
end;

procedure OpenBackupFolder(Sender: TObject);
var
  ResultCode: Integer;
begin
  if not ShellExec('open', BackupCleanupRoot, '', '', SW_SHOWNORMAL, ewNoWait, ResultCode) then
    MsgBox('Could not open the backup folder: ' + BackupCleanupRoot, mbError, MB_OK);
end;

procedure OfferBackupCleanup;
var
  Form: TSetupForm;
  FolderLabel, WarningLabel: TNewStaticText;
  FolderEdit: TNewEdit;
  CleanupCheck: TNewCheckBox;
  OpenButton, ContinueButton: TNewButton;
  Status, Path, SizeMB, Detail, ReportPath: String;
  Inspected, FolderExists, Ran: Boolean;
begin
  BackupCleanupRequested := False;
  if UninstallSilent then
    Exit;
  BackupCleanupRoot := ExpandConstant('{localappdata}\TheReachScreensaver\DeploymentBackups');
  BackupCleanupScript := ExpandConstant('{tmp}\Cleanup-ReachDeploymentBackups.ps1');
  FolderExists := DirExists(BackupCleanupRoot);
  Inspected := False;
  Detail := 'Backup inspection was unavailable. The backups will be kept.';
  SizeMB := 'unknown';
  Status := '';
  Path := '';
  if CopyFile(ExpandConstant('{app}\windows\Cleanup-ReachDeploymentBackups.ps1'), BackupCleanupScript, False) then
  begin
    ReportPath := ExpandConstant('{tmp}\ReachBackupInspection.txt');
    Ran := RunBackupCleanup('Inspect', ReportPath, Status, Path, SizeMB, Detail);
    if Ran then
    begin
      if CompareText(Path, BackupCleanupRoot) <> 0 then
      begin
        SizeMB := 'unknown';
        Detail := 'Backup inspection used a different folder: ' + Path + '. Backups will be kept.';
      end
      else if (Status = 'MISSING') and FolderExists then
      begin
        SizeMB := 'unknown';
        Detail := 'The backup folder exists, but PowerShell could not see it. Backups will be kept.';
      end
      else if (Status = 'EMPTY') and FolderExists then
        Detail := 'The backup folder exists but is empty.'
      else if (Status = 'MISSING') and not FolderExists then
        Detail := 'No retained upgrade backup folder was found.'
      else if (Status = 'READY') and not FolderExists then
      begin
        SizeMB := 'unknown';
        Detail := 'PowerShell found backups, but the dialog cannot see the folder. Backups will be kept.';
      end
      else if Status = 'READY' then
        Inspected := True
      else
      begin
        SizeMB := 'unknown';
        Detail := 'Unexpected backup inspection status: ' + Status + '. Backups will be kept.';
      end;
    end
    else
      SizeMB := 'unknown';
  end;

  Form := CreateCustomForm(ScaleX(570), ScaleY(228), False, False);
  try
    Form.Caption := 'The Reach - retained upgrade backups';

    FolderLabel := TNewStaticText.Create(Form);
    FolderLabel.Parent := Form;
    FolderLabel.Left := ScaleX(16);
    FolderLabel.Top := ScaleY(16);
    FolderLabel.Caption := 'Detected backup folder:';

    FolderEdit := TNewEdit.Create(Form);
    FolderEdit.Parent := Form;
    FolderEdit.Left := ScaleX(16);
    FolderEdit.Top := ScaleY(38);
    FolderEdit.Width := ScaleX(538);
    FolderEdit.ReadOnly := True;
    FolderEdit.Text := BackupCleanupRoot;

    CleanupCheck := TNewCheckBox.Create(Form);
    CleanupCheck.Parent := Form;
    CleanupCheck.Left := ScaleX(16);
    CleanupCheck.Top := ScaleY(78);
    CleanupCheck.Width := ScaleX(538);
    CleanupCheck.Caption := 'Also remove retained upgrade backups (' + SizeMB + ' MB)';
    CleanupCheck.Checked := False;
    CleanupCheck.Enabled := Inspected;

    WarningLabel := TNewStaticText.Create(Form);
    WarningLabel.Parent := Form;
    WarningLabel.Left := ScaleX(16);
    WarningLabel.Top := ScaleY(108);
    WarningLabel.Width := ScaleX(538);
    WarningLabel.Height := ScaleY(52);
    WarningLabel.AutoSize := False;
    WarningLabel.WordWrap := True;
    if Inspected then
      WarningLabel.Caption := 'Deleting these backups removes the ability to restore earlier installations. All other Reach user data stays in place.'
    else
      WarningLabel.Caption := Detail;

    OpenButton := TNewButton.Create(Form);
    OpenButton.Parent := Form;
    OpenButton.Left := ScaleX(16);
    OpenButton.Top := ScaleY(180);
    OpenButton.Width := ScaleX(140);
    OpenButton.Caption := 'Open Backup Folder';
    OpenButton.Enabled := FolderExists;
    OpenButton.OnClick := @OpenBackupFolder;

    ContinueButton := TNewButton.Create(Form);
    ContinueButton.Parent := Form;
    ContinueButton.Left := ScaleX(446);
    ContinueButton.Top := ScaleY(180);
    ContinueButton.Width := ScaleX(108);
    ContinueButton.Caption := 'Continue';
    ContinueButton.Default := True;
    ContinueButton.ModalResult := mrOk;

    if Form.ShowModal = mrOk then
      BackupCleanupRequested := CleanupCheck.Checked and Inspected;
  finally
    Form.Free;
  end;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpFinished then
  begin
    WizardForm.FinishedLabel.Caption :=
      'The Reach is installed!' + #13#10 + #13#10 +
      'To use it as your screensaver, select "Set The Reach as my Windows screensaver" below.' + #13#10 + #13#10 +
      'You can also select it later from Start > The Reach Screensaver > Select The Reach Screensaver.' + #13#10 + #13#10 +
      'Enjoy your journey!';
    WizardForm.AdjustLabelHeight(WizardForm.FinishedLabel);
    WizardForm.RunList.Top := WizardForm.FinishedLabel.Top + WizardForm.FinishedLabel.Height + ScaleY(10);
  end;
end;

function CheckReachProcesses(const DoBackup: Boolean): Boolean;
var
  ResultCode: Integer;
  ScriptPath: String;
  Parameters: String;
begin
  Result := False;
  if DoBackup then
  begin
    ExtractTemporaryFile('Prepare-ReachUpgrade.ps1');
    ScriptPath := ExpandConstant('{tmp}\Prepare-ReachUpgrade.ps1');
    Parameters := '-NoProfile -ExecutionPolicy Bypass -File "' + ScriptPath + '" -Backup';
  end
  else
  begin
    ScriptPath := ExpandConstant('{app}\windows\Prepare-ReachUpgrade.ps1');
    if not FileExists(ScriptPath) then
      Exit;
    Parameters := '-NoProfile -ExecutionPolicy Bypass -File "' + ScriptPath + '"';
  end;
  Result := Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
    Parameters, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
  ScriptPath: String;
  Parameters: String;
begin
  if CurStep <> ssPostInstall then
    Exit;
  ExtractTemporaryFile('Complete-ReachUpgrade.ps1');
  ScriptPath := ExpandConstant('{tmp}\Complete-ReachUpgrade.ps1');
  Parameters := '-NoProfile -ExecutionPolicy Bypass -File "' + ScriptPath + '" -Commit';
  if not Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
    Parameters, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) or (ResultCode <> 0) then
    RaiseException('The staged Reach payload could not be verified and installed. Previous files and recovery backups were preserved. Inspect DeploymentBackups before retrying.');
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if not CheckReachProcesses(True) then
    Result := 'Reach is running, or the existing per-user installation could not be backed up and verified. Close Reach and inspect the installation before retrying. No files were installed.';
end;

function InitializeUninstall: Boolean;
var
  ResultCode: Integer;
  ScriptPath: String;
  Parameters: String;
  Diagnostic: String;
  MessageText: String;
  DiagnosticText: AnsiString;
begin
  Result := False;
  ScriptPath := ExpandConstant('{app}\windows\Check-ReachUninstallSelection.ps1');
  if not FileExists(ScriptPath) then
    Exit;
  Parameters := '-NoProfile -ExecutionPolicy Bypass -File "' + ScriptPath +
    '" -InstalledScr "' + ExpandConstant('{app}\TheReachScreensaver.scr') +
    '" -DiagnosticFile "' + ExpandConstant('{tmp}\ReachUninstallSelection.txt') + '"';
  Diagnostic := ExpandConstant('{tmp}\ReachUninstallSelection.txt');
  if FileExists(Diagnostic) then
    DeleteFile(Diagnostic);
  if not Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
    Parameters, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) or (ResultCode <> 0) then
  begin
    MessageText := 'Could not safely verify the selected screensaver. No files were removed.';
    if FileExists(Diagnostic) then
      if LoadStringFromFile(Diagnostic, DiagnosticText) then
        MessageText := DiagnosticText;
    MsgBox(MessageText, mbError, MB_OK);
    Exit;
  end;
  if not CheckReachProcesses(False) then
  begin
    MsgBox('Close all Reach playback, preview, and Settings processes before uninstalling. No files were removed.', mbError, MB_OK);
    Exit;
  end;
  try
    OfferBackupCleanup;
  except
    BackupCleanupRequested := False;
    MsgBox('Backup cleanup options could not be shown. Backups will be kept; application uninstall can continue.', mbError, MB_OK);
  end;
  Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  Status, Path, SizeMB, Detail, ReportPath: String;
  Removed: Boolean;
begin
  if (CurUninstallStep <> usDone) or not BackupCleanupRequested then
    Exit;
  if DirExists(ExpandConstant('{app}')) or
    DirExists(ExpandConstant('{localappdata}\Programs\TheReachScreensaver.pending-v1.0.0')) or
    FileExists(ExpandConstant('{userprograms}\The Reach Screensaver\Select The Reach Screensaver.lnk')) or
    RegKeyExists(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{B98A81E3-3A1A-47B4-85FC-10AD5B48EF72}_is1') then
  begin
    MsgBox('Reach was not fully removed. Retained upgrade backups were kept at ' + BackupCleanupRoot, mbError, MB_OK);
    Exit;
  end;
  ReportPath := ExpandConstant('{tmp}\ReachBackupCleanup.txt');
  Detail := 'Cleanup could not complete. Inspect the backup folder manually.';
  try
    Removed := RunBackupCleanup('Remove', ReportPath, Status, Path, SizeMB, Detail);
  except
    Removed := False;
    Detail := GetExceptionMessage;
  end;
  if not Removed or (Status <> 'REMOVED') or (CompareText(Path, BackupCleanupRoot) <> 0) then
    MsgBox('Reach was uninstalled, but some or all retained backups remain.' + #13#10 +
      'Folder: ' + BackupCleanupRoot + #13#10 + Detail, mbError, MB_OK)
  else
    MsgBox('Retained upgrade backups were removed from ' + BackupCleanupRoot,
      mbInformation, MB_OK);
end;
