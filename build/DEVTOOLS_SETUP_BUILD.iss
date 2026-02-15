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
ArchitecturesInstallIn64BitMode=x64
AlwaysShowComponentsList=yes
PrivilegesRequired=admin

[Languages]
Name: "ptbr"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Types]
Name: "full"; Description: "Completa (WPF + CLI)"
Name: "custom"; Description: "Personalizada"; Flags: iscustom

[Components]
Name: "wpf"; Description: "Aplicação de Bandeja (WPF)"; Types: full custom
Name: "cli"; Description: "Interface de Terminal (CLI)"; Types: full custom

[Files]
; Instalamos o WPF em {app}\bin e o CLI em {app}\cli
Source: "{#WPF_DIR}\*"; DestDir: "{app}\bin"; Flags: recursesubdirs ignoreversion; Components: wpf
Source: "{#CLI_DIR}\*"; DestDir: "{app}\cli"; Flags: recursesubdirs ignoreversion; Components: cli

[Icons]
; --- ATALHOS WPF ---
; Menu Iniciar: Apontando para o EXE dentro da pasta bin
Name: "{group}\DevTools"; Filename: "{app}\bin\DevTools.Presentation.Wpf.exe"; WorkingDir: "{app}\bin"; Components: wpf
; Área de Trabalho
Name: "{commondesktop}\DevTools"; Filename: "{app}\bin\DevTools.Presentation.Wpf.exe"; WorkingDir: "{app}\bin"; Components: wpf

; --- ATALHOS CLI ---
; Menu Iniciar para o CLI
Name: "{group}\DevTools CLI"; Filename: "{app}\cli\DevTools.Cli.exe"; WorkingDir: "{app}\cli"; Components: cli
; Área de Trabalho para o CLI (Opcional, adicionei para garantir que você veja algo)
Name: "{commondesktop}\DevTools CLI"; Filename: "{app}\cli\DevTools.Cli.exe"; WorkingDir: "{app}\cli"; Components: cli

[Run]
Filename: "{app}\bin\DevTools.Presentation.Wpf.exe"; Description: "Iniciar DevTools agora"; Flags: nowait postinstall skipifsilent; Components: wpf

[Code]
// Lógica de desinstalação automática (Mantida para garantir limpeza)
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
    if (MsgBox('Uma versão anterior do DevTools foi detectada. Deseja removê-la antes de continuar?', mbConfirmation, MB_YESNO) = IDYES) then
    begin
      Exec(sUnInstallString, '/SILENT /NORESTART /SUPPRESSMSGBOXES', '', SW_SHOW, ewWaitUntilTerminated, iResultCode);
    end;
  end;
end;