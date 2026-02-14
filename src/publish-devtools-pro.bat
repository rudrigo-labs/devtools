@echo off
cls
setlocal EnableDelayedExpansion

REM ============================
REM CONFIG
REM ============================
set INNO_SCRIPT_DEFAULT=DEVTOOLS_PRD_SETUP.iss

REM ============================
REM TIMESTAMP
REM ============================
for /f %%i in ('powershell -NoProfile -Command "Get-Date -Format yyyy-MM-dd_HHmmss"') do set TS=%%i

REM ============================
REM MENU - PROJETO
REM ============================
echo ==========================================
echo       DEVTOOLS - PUBLISH (PRO)
echo ==========================================
echo.
echo Escolha o projeto:
echo 1 - Console (DevTools.Cli)
echo 2 - WPF     (DevTools.Pf.Wpf)
echo.
set /p projectChoice=Digite a opcao (1 ou 2): 

if "%projectChoice%"=="1" (
    set PROJECT_PATH=DevTools.Cli\DevTools.Cli.csproj
    set OUTPUT_ROOT=publish\CLI_PRD
    set PROJECT_TAG=CLI_PRD
)

if "%projectChoice%"=="2" (
    set PROJECT_PATH=DevTools.Pf.Wpf\DevTools.Pf.Wpf.csproj
    set OUTPUT_ROOT=publish\WPF_PRD
    set PROJECT_TAG=WPF_PRD
)

if not defined PROJECT_PATH (
    echo Opcao invalida.
    pause
    exit /b 1
)

REM ============================
REM MENU - TIPO PUBLISH
REM ============================
cls
echo ==========================================
echo Tipo de publish:
echo 1 - Framework-dependent (menor, precisa runtime)
echo 2 - Self-contained (maior, nao precisa runtime)
echo.
set /p publishType=Digite a opcao (1 ou 2): 

if "%publishType%" NEQ "1" if "%publishType%" NEQ "2" (
    echo Opcao invalida.
    pause
    exit /b 1
)

REM ============================
REM MENU - RUNTIME
REM ============================
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
    exit /b 1
)

REM ============================
REM MENU - SINGLE FILE
REM ============================
cls
echo ==========================================
echo Single File?
echo 1 - Sim
echo 2 - Nao
echo.
set /p singleChoice=Digite a opcao (1 ou 2): 

if "%singleChoice%"=="1" set SINGLEFILE=true
if "%singleChoice%"=="2" set SINGLEFILE=false

if not defined SINGLEFILE (
    echo Opcao invalida.
    pause
    exit /b 1
)

REM ============================
REM MENU - LIMPAR SAIDA
REM ============================
cls
echo ==========================================
echo Limpar pasta base de publish (%OUTPUT_ROOT%) antes?
echo 1 - Sim (apaga tudo dentro)
echo 2 - Nao
echo.
set /p cleanChoice=Digite a opcao (1 ou 2): 

if "%cleanChoice%"=="1" set CLEAN=true
if "%cleanChoice%"=="2" set CLEAN=false

if not defined CLEAN (
    echo Opcao invalida.
    pause
    exit /b 1
)

REM ============================
REM PREPARA PASTAS + LOG
REM ============================
set OUTPUT_DIR=%OUTPUT_ROOT%_%TS%
set LOG_DIR=logs
set LOG_FILE=%LOG_DIR%\publish_%PROJECT_TAG%_%RUNTIME%_%TS%.log

if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"

if "%CLEAN%"=="true" (
    if exist "%OUTPUT_ROOT%" (
        echo Limpando "%OUTPUT_ROOT%"...
        rmdir /s /q "%OUTPUT_ROOT%"
    )
)

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"

REM ============================
REM PUBLISH
REM ============================
cls
echo ==========================================
echo Publicando: %PROJECT_PATH%
echo Tipo: %publishType%   Runtime: %RUNTIME%   SingleFile: %SINGLEFILE%
echo Saida: %OUTPUT_DIR%
echo Log:   %LOG_FILE%
echo ==========================================
echo.

echo ==== dotnet --info ==== > "%LOG_FILE%"
dotnet --info >> "%LOG_FILE%" 2>&1
echo.>> "%LOG_FILE%"

echo ==== dotnet publish ==== >> "%LOG_FILE%"

if "%publishType%"=="1" (
    dotnet publish "%PROJECT_PATH%" ^
        -c Release ^
        -r %RUNTIME% ^
        --self-contained false ^
        -o "%OUTPUT_DIR%" ^
        -p:PublishSingleFile=%SINGLEFILE% ^
        >> "%LOG_FILE%" 2>&1
)

if "%publishType%"=="2" (
    dotnet publish "%PROJECT_PATH%" ^
        -c Release ^
        -r %RUNTIME% ^
        --self-contained true ^
        -o "%OUTPUT_DIR%" ^
        -p:PublishSingleFile=%SINGLEFILE% ^
        -p:IncludeNativeLibrariesForSelfExtract=true ^
        >> "%LOG_FILE%" 2>&1
)

if errorlevel 1 (
    echo.
    echo ==========================================
    echo ERRO no publish. Veja o log:
    echo %LOG_FILE%
    echo ==========================================
    pause
    exit /b 1
)

echo.
echo ==========================================
echo PUBLISH OK!
echo Saida: %OUTPUT_DIR%
echo Log:   %LOG_FILE%
echo ==========================================
echo.

REM ============================
REM OPCIONAL: COMPILAR INNO SETUP
REM ============================
echo Quer compilar o instalador no Inno Setup agora?
echo 1 - Sim
echo 2 - Nao
echo.
set /p innoChoice=Digite a opcao (1 ou 2): 

if "%innoChoice%"=="2" (
    echo OK. Instalador nao compilado.
    pause
    exit /b 0
)

if "%innoChoice%" NEQ "1" (
    echo Opcao invalida.
    pause
    exit /b 1
)

REM ============================
REM AUTO-DETECCAO DO ISCC
REM ============================
set ISCC_PATH=
for %%p in (ISCC.exe) do set ISCC_PATH=%%~$PATH:p

if not defined ISCC_PATH (
    if exist "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" set ISCC_PATH=C:\Program Files (x86)\Inno Setup 6\ISCC.exe
)

if not defined ISCC_PATH (
    if exist "C:\Program Files\Inno Setup 6\ISCC.exe" set ISCC_PATH=C:\Program Files\Inno Setup 6\ISCC.exe
)

if not defined ISCC_PATH (
    echo.
    echo ==========================================
    echo NAO ACHEI O ISCC.exe
    echo - Instale o Inno Setup 6
    echo - Ou adicione ISCC.exe no PATH
    echo ==========================================
    pause
    exit /b 1
)

REM Script .iss
set INNO_SCRIPT=%INNO_SCRIPT_DEFAULT%
if not exist "%INNO_SCRIPT%" (
    echo.
    echo Script .iss nao encontrado: %INNO_SCRIPT%
    pause
    exit /b 1
)

echo.
echo ==========================================
echo Compilando instalador (Inno Setup)...
echo ISCC:  %ISCC_PATH%
echo Script:%INNO_SCRIPT%
echo ==========================================
echo.

"%ISCC_PATH%" "%INNO_SCRIPT%" >> "%LOG_FILE%" 2>&1

if errorlevel 1 (
    echo.
    echo ==========================================
    echo ERRO ao compilar o instalador. Veja o log:
    echo %LOG_FILE%
    echo ==========================================
    pause
    exit /b 1
)

echo.
echo ==========================================
echo INSTALADOR OK!
echo (Detalhes no log)
echo %LOG_FILE%
echo ==========================================
pause
exit /b 0
