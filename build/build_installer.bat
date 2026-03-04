@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
pushd "%SCRIPT_DIR%.." >nul
set "REPO_ROOT=%CD%"
popd

:: Ferramentas
set "ISCC=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
set "WPF_PROJ=%REPO_ROOT%\src\Presentation\DevTools.Presentation.Wpf\DevTools.Presentation.Wpf.csproj"

:: Icone para o rosto do Instalador (ApplicationIcon)
set "ICO_APP=%REPO_ROOT%\src\Presentation\DevTools.Presentation.Wpf\Assets\application.ico"
set "MANUAL_FILE=%REPO_ROOT%\MANUAL.md"

:: Versao (pode ser passada como primeiro argumento)
set "APP_VERSION=%~1"
if "%APP_VERSION%"=="" set "APP_VERSION=1.0.0"

:: Saidas
set "OUT_DIR=%SCRIPT_DIR%out"
set "WPF_OUT=%OUT_DIR%\wpf"
set "INST_OUT=%OUT_DIR%\installer"

if not exist "%ISCC%" (
    echo [Erro] ISCC.exe nao encontrado em "%ISCC%"
    echo [Dica] Instale o Inno Setup 6 ou ajuste o caminho no script.
    exit /b 1
)

if not exist "%WPF_PROJ%" (
    echo [Erro] Projeto WPF nao encontrado em "%WPF_PROJ%"
    exit /b 1
)

if not exist "%ICO_APP%" (
    echo [Erro] Icone nao encontrado em "%ICO_APP%"
    exit /b 1
)

if not exist "%MANUAL_FILE%" (
    echo [Erro] Manual nao encontrado em "%MANUAL_FILE%"
    exit /b 1
)

echo [Build] Limpando pastas...
if exist "%OUT_DIR%" rd /s /q "%OUT_DIR%"
mkdir "%WPF_OUT%" "%INST_OUT%" 2>nul

echo [1/2] Publicando WPF (Release, win-x64)...
dotnet publish "%WPF_PROJ%" -c Release -r win-x64 --self-contained false -o "%WPF_OUT%"
if errorlevel 1 (
    echo [Erro] Falha ao publicar WPF.
    exit /b 1
)

echo [2/2] Criando instalador (Inno Setup)...
"%ISCC%" /dAPP_VERSION="%APP_VERSION%" /dWPF_DIR="%WPF_OUT%" /dOUT_DIR="%INST_OUT%" /dICO_APP="%ICO_APP%" /dMANUAL_FILE="%MANUAL_FILE%" "%SCRIPT_DIR%DEVTOOLS_SETUP_BUILD.iss"
if errorlevel 1 (
    echo [Erro] Falha ao gerar instalador.
    exit /b 1
)

echo [OK] Instalador gerado em "%INST_OUT%".
if exist "%INST_OUT%" start "" "%INST_OUT%"

if /I "%CI%"=="true" exit /b 0
pause
