@echo off
cls
setlocal EnableDelayedExpansion

echo ==========================================
echo           DEVTOOLS - PUBLISH
echo ==========================================
echo.

echo Escolha o projeto:
echo 1 - Console (DevTools.Cli)
echo 2 - WPF     (DevTools.Pf.Wpf)
echo.
set /p projectChoice=Digite a opcao (1 ou 2): 

if "%projectChoice%"=="1" (
    set PROJECT_PATH=DevTools.Cli\DevTools.Cli.csproj
    set OUTPUT_DIR=publish\CLI_PRD
    set EXE_NAME=DevTools.Cli.exe
)

if "%projectChoice%"=="2" (
    set PROJECT_PATH=DevTools.Pf.Wpf\DevTools.Pf.Wpf.csproj
    set OUTPUT_DIR=publish\WPF_PRD
    set EXE_NAME=DevTools.Pf.Wpf.exe
)

if not defined PROJECT_PATH (
    echo Opcao invalida.
    pause
    exit /b
)

cls
echo ==========================================
echo Tipo de publish:
echo 1 - Framework-dependent (menor, precisa runtime)
echo 2 - Self-contained (maior, nao precisa runtime)
echo.
set /p publishType=Digite a opcao (1 ou 2): 

cls
echo ==========================================
echo Arquitetura:
echo 1 - win-x64
echo 2 - win-x86
echo.
set /p archChoice=Digite a opcao (1 ou 2): 

if "%archChoice%"=="1" set RUNTIME=win-x64
if "%archChoice%"=="2" set RUNTIME=win-x86

if not defined RUNTIME (
    echo Arquitetura invalida.
    pause
    exit /b
)

cls
echo ==========================================
echo Single File?
echo 1 - Sim
echo 2 - Nao
echo.
set /p singleChoice=Digite a opcao (1 ou 2): 

if "%singleChoice%"=="1" (
    set SINGLEFILE=true
) else (
    set SINGLEFILE=false
)

echo.
echo ==========================================
echo Iniciando publish...
echo ==========================================
echo.

if "%publishType%"=="1" (
    dotnet publish "%PROJECT_PATH%" ^
        -c Release ^
        -r %RUNTIME% ^
        --self-contained false ^
        -o "%OUTPUT_DIR%" ^
        -p:PublishSingleFile=%SINGLEFILE%
)

if "%publishType%"=="2" (
    dotnet publish "%PROJECT_PATH%" ^
        -c Release ^
        -r %RUNTIME% ^
        --self-contained true ^
        -o "%OUTPUT_DIR%" ^
        -p:PublishSingleFile=%SINGLEFILE% ^
        -p:IncludeNativeLibrariesForSelfExtract=true
)

echo.
echo ==========================================
echo PUBLISH FINALIZADO!
echo Saida em: %OUTPUT_DIR%
echo ==========================================
echo.
pause
