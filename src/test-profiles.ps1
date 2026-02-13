$ErrorActionPreference = "Stop"

Write-Host "1. Testando criação de perfil (dry-run)..." -ForegroundColor Cyan
# Cria um diretório de teste se não existir
if (!(Test-Path "C:\Temp\TestProfile")) { mkdir "C:\Temp\TestProfile" | Out-Null }

# Roda o comando rename definindo inputs e salvando perfil
# --non-interactive força erro se faltar algo, mas aqui passamos tudo
dotnet run --project Cli/DevTools.Cli/DevTools.Cli.csproj -- rename --root "C:\Temp\TestProfile" --old "foo" --new "bar" --dry-run true --save-profile "SmokeTest" --non-interactive

Write-Host "2. Verificando se arquivo de perfil existe..." -ForegroundColor Cyan
if (!(Test-Path "devtools.rename.json")) {
    Write-Error "Arquivo de perfil devtools.rename.json não foi criado!"
} else {
    Write-Host "Arquivo de perfil encontrado." -ForegroundColor Green
}

Write-Host "3. Testando uso do perfil..." -ForegroundColor Cyan
# Roda novamente usando apenas o perfil carregado
# Captura output para verificar se os parâmetros foram aplicados
$output = dotnet run --project Cli/DevTools.Cli/DevTools.Cli.csproj -- rename --profile "SmokeTest" --non-interactive --dry-run true

if ($output -match "foo" -and $output -match "bar") {
    Write-Host "Sucesso: Perfil carregado e parâmetros 'foo'/'bar' detectados na execução!" -ForegroundColor Green
} else {
    Write-Error "Falha: Output não contém os parâmetros esperados do perfil."
}

Write-Host "Smoke Test Concluído com Sucesso!" -ForegroundColor Green
