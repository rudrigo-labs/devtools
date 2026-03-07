# V2 - Libraries only (root-level projects, no src/) | .NET 10 / VS 2026 (.slnx)
# Rode dentro da pasta V2

$ErrorActionPreference = "Stop"

$solutionName = "DevTools"
$solutionFile = "$solutionName.slnx"

# Tools (libraries)
$tools = @(
  "DevTools.Harvest",
  "DevTools.Image",
  "DevTools.Migrations",
  "DevTools.Ngrok",
  "DevTools.Notes",
  "DevTools.Organizer",
  "DevTools.Rename",
  "DevTools.SearchText",
  "DevTools.Snapshot",
  "DevTools.SSHTunnel",
  "DevTools.Utf8Convert"
)

# 1) Solution (gera .slnx por padrão no .NET 10)
dotnet new sln --name $solutionName

# 2) Core shared (opcional, mas recomendado)
if (!(Test-Path "DevTools.Core")) {
  dotnet new classlib -n "DevTools.Core" | Out-Null
}

# 3) Criar classlibs por tool (na raiz)
foreach ($t in $tools) {
  if (!(Test-Path $t)) {
    dotnet new classlib -n $t | Out-Null
  }
}

# 4) Adicionar tudo na solution
$csprojs = @(".\DevTools.Core\DevTools.Core.csproj")
foreach ($t in $tools) { $csprojs += ".\$t\$t.csproj" }

dotnet sln ".\$solutionFile" add $csprojs

# 5) (Opcional) Referenciar Core em todas as tools
foreach ($t in $tools) {
  dotnet add ".\$t\$t.csproj" reference ".\DevTools.Core\DevTools.Core.csproj"
}

# 6) Build pra validar
dotnet build ".\$solutionFile"
