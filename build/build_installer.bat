@echo off
setlocal EnableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
pushd "%SCRIPT_DIR%.." >nul
set "REPO_ROOT=%CD%"
popd

:: Ferramentas
set "ISCC=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
set "WPF_PROJ=%REPO_ROOT%\src\Presentation\DevTools.Presentation.Wpf\DevTools.Presentation.Wpf.csproj"
set "CLI_PROJ=%REPO_ROOT%\src\Cli\DevTools.Cli\DevTools.Cli.csproj"

:: Ícone para o rosto do Instalador (ApplicationIcon)
set "ICO_APP=%REPO_ROOT%\src\Presentation\DevTools.Presentation.Wpf\Assets\application.ico"

:: Saídas
set "OUT_DIR=%SCRIPT_DIR%out"
set "WPF_OUT=%OUT_DIR%\wpf"
set "CLI_OUT=%OUT_DIR%\cli"
set "INST_OUT=%OUT_DIR%\installer"

echo [Build] Limpando pastas...
if exist "%OUT_DIR%" rd /s /q "%OUT_DIR%"
mkdir "%WPF_OUT%" "%CLI_OUT%" "%INST_OUT%" 2>nul

echo [1/2] Publicando WPF (Release)...
dotnet publish "%WPF_PROJ%" -c Release -r win-x64 --self-contained false -o "%WPF_OUT%"

echo [2/2] Publicando CLI (Release)...
dotnet publish "%CLI_PROJ%" -c Release -r win-x64 --self-contained false -o "%CLI_OUT%"

echo [Inno] Criando instalador...
"%ISCC%" /dAPP_VERSION="1.0.0" /dWPF_DIR="%WPF_OUT%" /dCLI_DIR="%CLI_OUT%" /dOUT_DIR="%INST_OUT%" /dICO_APP="%ICO_APP%" "%SCRIPT_DIR%DEVTOOLS_SETUP_BUILD.iss"

if %ERRORLEVEL% EQU 0 start "" "%INST_OUT%"
pause