#define AppVersion "1.0.0"
#define PackageRoot "..\..\artifacts\v1.0.0\TheReachScreensaver-1.0.0-win-x64"

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
OutputDir=..\..\artifacts\v1.0.0
OutputBaseFilename=TheReachScreensaver-1.0.0-Setup
OutputManifestFile=TheReachScreensaver-1.0.0-Setup-Manifest.txt
Compression=lzma2
SolidCompression=yes
VersionInfoVersion=1.0.0.0
VersionInfoProductVersion=1.0.0.0
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

[Run]
Filename: "{app}\TheReachScreensaver.exe"; Parameters: "/c"; Description: "Open The Reach Screensaver Settings"; Flags: postinstall skipifsilent unchecked nowait

[Code]
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
begin
  Result := False;
  ScriptPath := ExpandConstant('{app}\windows\Check-ReachUninstallSelection.ps1');
  if not FileExists(ScriptPath) then
    Exit;
  Parameters := '-NoProfile -ExecutionPolicy Bypass -File "' + ScriptPath +
    '" -InstalledScr "' + ExpandConstant('{app}\TheReachScreensaver.scr') + '"';
  if not Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
    Parameters, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) or (ResultCode <> 0) then
  begin
    MsgBox('Reach may be selected as the Windows screen saver, or its selection could not be verified. Choose another screen saver in Windows Screen Saver Settings before uninstalling.', mbError, MB_OK);
    Exit;
  end;
  if not CheckReachProcesses(False) then
  begin
    MsgBox('Close all Reach playback, preview, and Settings processes before uninstalling. No files were removed.', mbError, MB_OK);
    Exit;
  end;
  Result := True;
end;
