# DevTools - Documentacao Tecnica

## Visao geral

DevTools e uma aplicacao WPF modular para produtividade tecnica no Windows.
O foco atual e shell unico em estilo IDE, com roteamento de ferramentas e execucao via bandeja.

Stack atual:

- .NET 10 (`net10.0-windows`)
- WPF + MaterialDesignThemes
- Persistencia em JSON (padrao) ou SQLite (opcional)

## Estrutura do repositorio

- `src/Core/DevTools.Core`: contratos e modelos base
- `src/Tools/*`: engines por ferramenta
- `src/Presentation/DevTools.Presentation.Wpf`: shell, views, tema, tray, storage
- `src/Tools/DevTools.Tests`: testes de integracao/smoke

## Arquitetura de execucao

### Shell e navegacao

- `MainWindow` funciona como shell principal com abas:
1. Ferramentas
2. Jobs
3. Configuracoes

### Tool routing

O roteamento e centralizado via:

- `ToolDescriptor`
- `ToolRegistry`
- `ToolRouter`
- strategies por modo de abertura:
1. `EmbeddedTabLaunchStrategy`
2. `DetachedWindowLaunchStrategy`
3. `BackgroundOnlyLaunchStrategy`

O `TrayService` registra as tools e aciona o router.

### Jobs e execucao assincrona

`JobManager` controla tarefas em background:

- cria e acompanha jobs
- publica progresso/log/status
- suporta cancelamento individual ou em lote
- integra com UI (aba Jobs) e tray

## Persistencia

Backends suportados:

1. JSON (default)
2. SQLite (habilitado por `DEVTOOLS_STORAGE_BACKEND=sqlite`)

Notas `.txt/.md` continuam em arquivos fisicos no disco.

## Google Drive (Notes)

Fluxo atual:

1. salvar nota localmente (sempre)
2. se configurado e ativo, sincronizar para Google Drive

Configuracao de credenciais via UI (sem `credentials.json` manual na pasta do app).

## Empacotamento e distribuicao

Fluxo oficial:

```powershell
build\build_installer.bat 1.0.0
```

Script publica WPF e gera instalador Inno Setup (`DEVTOOLS_SETUP_BUILD.iss`), incluindo manual.

## Testes

Principal suite:

```powershell
dotnet test src/Tools/DevTools.Tests/DevTools.Tests.csproj -c Debug
```

Cobertura de referencia:

- `docs/INTEGRATION_TEST_COVERAGE.md`

## Observacoes de manutencao

- CLI e considerado obsoleto para entrega oficial.
- Documentacao de uso deve permanecer em `MANUAL.md` (raiz).
- Documentos legados devem ser movidos para `docs/_obsolete`.
