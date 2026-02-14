@echo off
setlocal EnableDelayedExpansion

REM ============================================================
REM RELEASE PRD - ONE CLICK (SUA ESTRUTURA)
REM - Publish CLI + WPF (Release / win-x64 / self-contained / single-file)
REM - Saída estável: publish\CLI_PRD e publish\WPF_PRD
REM - Histórico: releases\YYYY-MM-DD_HHMMSS\...
REM - Log: logs\release_prd_*.log
REM - Compila Inno Setup e gera installer\DEVTOOLS_PRD_SETUP_*.exe
REM ============================================================

REM ---- PROJETOS (SUA ESTRUTURA DE PASTAS)
set CLI_PROJECT=Cli\DevTools.Cli\DevTools.Cli.csproj
set WPF_PROJECT=Presentation\DevTools.Presentation.Wpf\DevTools.Presentation.Wpf.csproj

REM ---- SAIDAS FIXAS (o .iss aponta pra elas)
set OUT_CLI=publish\CLI_PRD
set OUT_WPF=publish\WPF_PRD

REM ---- INNO SCRIPT
set ISS=DEVTOOLS_PRD_SETUP.iss

REM (opcional) ícone do instalador: coloque um .ico e descomente
REM set SETUP_ICON=Presentation\DevTools.Presentation.Wpf\Assets\devtools.ico

set CONFIG=Release
set RUNTIME=win-x64
set SELFCONTAINED=true
set SINGLEFILE=true

REM ---- TIMESTAMP
for /f %%i in ('powershell -NoProfile -Command "Get-Date -Format yyyy-MM-dd_HHmmss"') do set TS=%%i

set LOG_DIR=logs
set REL_DIR=releases\%TS%
set LOG_FILE=%LOG_DIR%\release_prd_%TS%.log

if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"
if not exist "%REL_DIR%" mkdir "%REL_DIR%"

echo ==== RELEASE PRD ONECLICK START ==== > "%LOG_FILE%"
echo Timestamp: %TS%>> "%LOG_FILE%"
echo.>> "%LOG_FILE%"

REM ---- Auto-detect ISCC.exe
set ISCC_PATH=
for %%p in (ISCC.exe) do set ISCC_PATH=%%~$PATH:p

if not defined ISCC_PATH (
  if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" set ISCC_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe
)
if not defined ISCC_PATH (
  if exist "C:\Program Files\Inno Setup 6\ISCC.exe" set ISCC_PATH=C:\Program Files\Inno Setup 6\ISCC.exe
)

if not defined ISCC_PATH (
  echo ERRO: ISCC.exe nao encontrado.>> "%LOG_FILE%"
  echo.
  echo ERRO: ISCC.exe (Inno Setup) nao encontrado.
  echo Instale o Inno Setup 6 ou coloque o ISCC no PATH.
  pause
  exit /b 1
)

if not exist "%ISS%" (
  echo ERRO: .iss nao encontrado: %ISS%>> "%LOG_FILE%"
  echo.
  echo ERRO: nao achei %ISS% na raiz.
  pause
  exit /b 1
)

REM ---- Limpa outputs estaveis
if exist "%OUT_CLI%" rmdir /s /q "%OUT_CLI%"
if exist "%OUT_WPF%" rmdir /s /q "%OUT_WPF%"

mkdir "%OUT_CLI%" >nul 2>&1
mkdir "%OUT_WPF%" >nul 2>&1

REM ---- dotnet info
echo ==== dotnet --info ====>> "%LOG_FILE%"
dotnet --info >> "%LOG_FILE%" 2>&1
echo.>> "%LOG_FILE%"

REM ---- Publish CLI
echo ==== PUBLISH CLI ====>> "%LOG_FILE%"
dotnet publish "%CLI_PROJECT%" ^
  -c %CONFIG% ^
  -r %RUNTIME% ^
  --self-contained %SELFCONTAINED% ^
  -o "%OUT_CLI%" ^
  -p:PublishSingleFile=%SINGLEFILE% ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  >> "%LOG_FILE%" 2>&1

if errorlevel 1 (
  echo ERRO: publish CLI falhou.>> "%LOG_FILE%"
  echo.
  echo ERRO no publish do CLI. Veja: %LOG_FILE%
  pause
  exit /b 1
)

REM ---- Publish WPF
echo ==== PUBLISH WPF ====>> "%LOG_FILE%"
dotnet publish "%WPF_PROJECT%" ^
  -c %CONFIG% ^
  -r %RUNTIME% ^
  --self-contained %SELFCONTAINED% ^
  -o "%OUT_WPF%" ^
  -p:PublishSingleFile=%SINGLEFILE% ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  >> "%LOG_FILE%" 2>&1

if errorlevel 1 (
  echo ERRO: publish WPF falhou.>> "%LOG_FILE%"
  echo.
  echo ERRO no publish do WPF. Veja: %LOG_FILE%
  pause
  exit /b 1
)

REM ---- Descobre versão do EXE do WPF (pra nomear instalador)
set APP_VERSION=1.0.0
for /f %%v in ('powershell -NoProfile -Command ^
  "$p = Resolve-Path '%OUT_WPF%\DevTools.Presentation.Wpf.exe' -ErrorAction SilentlyContinue; ^
   if($p){ (Get-Item $p).VersionInfo.ProductVersion } else { '1.0.0' }"') do set APP_VERSION=%%v

echo ==== VERSION ====>> "%LOG_FILE%"
echo APP_VERSION=%APP_VERSION%>> "%LOG_FILE%"

REM ---- Arquiva histórico
echo ==== ARCHIVE ====>> "%LOG_FILE%"
mkdir "%REL_DIR%\CLI" >nul 2>&1
mkdir "%REL_DIR%\WPF" >nul 2>&1
xcopy "%OUT_CLI%\*" "%REL_DIR%\CLI\" /E /I /H /Y >> "%LOG_FILE%" 2>&1
xcopy "%OUT_WPF%\*" "%REL_DIR%\WPF\" /E /I /H /Y >> "%LOG_FILE%" 2>&1

REM ---- Compila Inno Setup
echo ==== INNO SETUP ====>> "%LOG_FILE%"
set ISCC_ARGS=/DAPP_VERSION=%APP_VERSION% /DCLI_DIR="%CD%\%OUT_CLI%" /DWPF_DIR="%CD%\%OUT_WPF%"

if defined SETUP_ICON (
  set ISCC_ARGS=!ISCC_ARGS! /DSETUP_ICON="%CD%\%SETUP_ICON%"
)

"%ISCC_PATH%" "%ISS%" %ISCC_ARGS% >> "%LOG_FILE%" 2>&1

if errorlevel 1 (
  echo ERRO: Inno Setup falhou.>> "%LOG_FILE%"
  echo.
  echo ERRO ao compilar instalador. Veja: %LOG_FILE%
  pause
  exit /b 1
)

echo ==== DONE ====>> "%LOG_FILE%"

echo.
echo ==========================================
echo RELEASE PRD GERADO!
echo - Publish:
echo   %OUT_CLI%
echo   %OUT_WPF%
echo - Historico:
echo   %REL_DIR%
echo - Instalador:
echo   installer\
echo - Log:
echo   %LOG_FILE%
echo ==========================================
pause
exit /b 0
