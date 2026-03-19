#ifndef APP_EXE
  #define APP_EXE "DevTools.Host.Wpf.exe"
#endif

[Setup]
AppId={{7B6A2B8B-3B7F-4D8E-9C3F-5F6C7A8B9C10}}
AppName=DevTools
AppVersion={#APP_VERSION}
DefaultDirName={autopf}\DevTools
DefaultGroupName=DevTools
OutputDir={#OUT_DIR}
OutputBaseFilename=DevTools_Setup
SetupIconFile={#ICO_APP}
Compression=lzma2
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64compatible
AlwaysShowComponentsList=yes
PrivilegesRequired=admin

[Languages]
Name: "ptbr"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Types]
Name: "full"; Description: "Completa (WPF)"
Name: "custom"; Description: "Personalizada"; Flags: iscustom

[Components]
Name: "wpf"; Description: "Aplicacao de Bandeja (WPF)"; Types: full custom

[Files]
Source: "{#WPF_DIR}\*"; DestDir: "{app}\bin"; Flags: recursesubdirs ignoreversion; Components: wpf
Source: "{#MANUAL_FILE}"; DestDir: "{app}\docs"; DestName: "MANUAL_DEVTOOLS.md"; Flags: ignoreversion; Components: wpf

[Icons]
Name: "{group}\DevTools"; Filename: "{app}\bin\{#APP_EXE}"; WorkingDir: "{app}\bin"; Components: wpf
Name: "{commondesktop}\DevTools"; Filename: "{app}\bin\{#APP_EXE}"; WorkingDir: "{app}\bin"; Components: wpf
Name: "{group}\Manual do DevTools"; Filename: "{app}\docs\MANUAL_DEVTOOLS.md"; WorkingDir: "{app}\docs"; Components: wpf

[Run]
Filename: "{app}\bin\{#APP_EXE}"; Description: "Iniciar DevTools agora"; Flags: nowait postinstall skipifsilent; Components: wpf

[Code]
function GetUninstallString(): String;
var
  sUninstPath: String;
  sUnInstallString: String;
begin
  sUninstPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}_is1');
  sUnInstallString := '';
  if not RegQueryStringValue(HKLM, sUninstPath, 'UninstallString', sUnInstallString) then
    RegQueryStringValue(HKCU, sUninstPath, 'UninstallString', sUnInstallString);
  Result := sUnInstallString;
end;

function InitializeSetup(): Boolean;
var
  iResultCode: Integer;
  sUnInstallString: String;
begin
  Result := True;
  sUnInstallString := GetUninstallString();
  if sUnInstallString <> '' then
  begin
    sUnInstallString := RemoveQuotes(sUnInstallString);
    if (MsgBox('Uma versao anterior do DevTools foi detectada. Deseja remove-la antes de continuar?', mbConfirmation, MB_YESNO) = IDYES) then
    begin
      Exec(sUnInstallString, '/SILENT /NORESTART /SUPPRESSMSGBOXES', '', SW_SHOW, ewWaitUntilTerminated, iResultCode);
    end;
  end;
end;

