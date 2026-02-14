#define MyAppName "DevTools"
#define MyPublisher "DevTools"
#define MyAppExeWpf "DevTools.Presentation.Wpf.exe"
#define MyAppExeCli "DevTools.Cli.exe"

; Inputs (passados pelo BAT via /D)
; /DAPP_VERSION=1.2.3
; /DCLI_DIR=...\publish\CLI_PRD
; /DWPF_DIR=...\publish\WPF_PRD
; /DSETUP_ICON=...\Presentation\DevTools.Presentation.Wpf\Assets\devtools.ico  (opcional)

#ifndef CLI_DIR
  #define CLI_DIR SourcePath + "\publish\CLI_PRD"
#endif

#ifndef WPF_DIR
  #define WPF_DIR SourcePath + "\publish\WPF_PRD"
#endif

#ifndef APP_VERSION
  #define WpfExePath WPF_DIR + "\" + MyAppExeWpf
  #if FileExists(WpfExePath)
    #define APP_VERSION GetFileVersion(WpfExePath)
  #else
    #define APP_VERSION "1.0.0"
  #endif
#endif

[Setup]
AppId={{7B6A2B8B-3B7F-4D8E-9C3F-5F6C7A8B9C10}}
AppName={#MyAppName}
AppVersion={#APP_VERSION}
AppPublisher={#MyPublisher}

DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}

OutputDir={#SourcePath}\installer
OutputBaseFilename=DEVTOOLS_PRD_SETUP_{#APP_VERSION}_win-x64

Compression=lzma2
SolidCompression=yes

ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

WizardStyle=modern
PrivilegesRequired=admin
SetupLogging=yes

UninstallDisplayName={#MyAppName} {#APP_VERSION}
UninstallDisplayIcon={app}\wpf\{#MyAppExeWpf}

#ifdef SETUP_ICON
  SetupIconFile={#SETUP_ICON}
#endif

[Languages]
Name: "ptbr"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na Área de Trabalho (WPF)"; GroupDescription: "Atalhos:"; Flags: unchecked
Name: "startmenuicon"; Description: "Criar atalhos no Menu Iniciar"; GroupDescription: "Atalhos:"; Flags: checkedonce

[Dirs]
Name: "{app}\cli"
Name: "{app}\wpf"

[Files]
Source: "{#CLI_DIR}\*"; DestDir: "{app}\cli"; Flags: recursesubdirs ignoreversion
Source: "{#WPF_DIR}\*"; DestDir: "{app}\wpf"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\DevTools (WPF)"; Filename: "{app}\wpf\{#MyAppExeWpf}"; Tasks: startmenuicon
Name: "{group}\DevTools (CLI)"; Filename: "{app}\cli\{#MyAppExeCli}"; Tasks: startmenuicon
Name: "{commondesktop}\DevTools"; Filename: "{app}\wpf\{#MyAppExeWpf}"; Tasks: desktopicon

[Run]
Filename: "{app}\wpf\{#MyAppExeWpf}";
Description: "Abrir DevTools agora";
Flags: nowait postinstall skipifsilent
