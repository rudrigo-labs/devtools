# DevTools - Documento Tecnico (Detalhado)

Data de referencia: 2026-03-05

## 1. Escopo tecnico

Este documento descreve a arquitetura atual do DevTools, responsabilidades de cada camada, fluxos de dados e pontos de extensao.

## 2. Arquitetura de alto nivel

Camadas:

1. `Core` (contratos, modelos e resultado padrao)
2. `Tools` (regras de negocio por ferramenta)
3. `Presentation.Wpf` (shell, UI, tema, roteamento, persistencia)

Diretorios principais:

- `src/Core/DevTools.Core`
- `src/Tools/*`
- `src/Presentation/DevTools.Presentation.Wpf`

## 3. Shell, navegacao e roteamento

## 3.1 Shell

- janela principal: `MainWindow`
- abas principais: ferramentas, jobs, configuracoes

## 3.2 Roteamento

Componentes:

- `ToolDescriptor`
- `ToolRegistry`
- `ToolRouter`
- launch strategies:
  - `EmbeddedTabLaunchStrategy`
  - `DetachedWindowLaunchStrategy`
  - `BackgroundOnlyLaunchStrategy`

Servico orquestrador:

- `TrayService`

Responsabilidade:

- abrir ferramenta pela estrategia correta
- integrar com menu de bandeja
- manter ciclo de vida das janelas

## 4. Jobs e execucao assincrona

- `JobManager` registra inicio, progresso e fim de tarefas
- ferramental envia jobs para manter UI responsiva
- aba `Jobs` apresenta status consolidado

## 5. Persistencia

## 5.1 Selecao de backend

- enum: `StorageBackend` (`Json`, `Sqlite`)
- resolver: `StorageBackendResolver` via `DEVTOOLS_STORAGE_BACKEND`

## 5.2 Settings globais

- facade: `ConfigService`
- contrato: `ISettingsStore`
- implementacoes:
  - `JsonSettingsStore`
  - `SqliteSettingsStore`

## 5.3 Perfil por ferramenta

- facade: `ProfileManager`
- suporte de UI: `ProfileUIService`
- store JSON e SQLite

## 5.4 Preferencias da aplicacao

- `SettingsService`
- arquivo `%AppData%\DevTools\settings.json`

## 5.5 Notas

- conteudo da nota permanece em arquivo fisico local
- metadados e configuracoes usam camada de persistencia

## 6. Ferramentas - status tecnico

## 6.1 Notes

- engine: `DevTools.Notes`
- create/read/save/list/export/import
- sync Google Drive opcional no WPF

## 6.2 Ngrok

Estrutura principal:

- `NgrokSetupService`
- `NgrokConfigEngine`
- `NgrokTunnelEngine`
- `NgrokEnvironmentService`
- `NgrokEngine` (API/processo)

Capacidades implementadas:

1. detectar executavel (`PATH` ou caminho configurado)
2. detectar config YAML do ngrok
3. ler `authtoken` existente
4. iniciar/parar tunel
5. listar tuneis via API local `127.0.0.1:4040`

Limitacao de validacao automatica:

- sem `ngrok.exe` instalado no ambiente, E2E real nao e coberto pelos testes automatizados.

## 6.3 SSH Tunnel

- engine dedicada em `DevTools.SSHTunnel`
- integracao com `TunnelService`
- controle de ciclo de vida via shell/tray

## 6.4 Demais ferramentas

- Organizer, Harvest, SearchText, Rename, Snapshot, Utf8Convert, Image, Migrations
- cada uma com engine e request/response proprios

## 7. UI/UX e tema

- estilos centralizados em `Theme/*`
- componentes reutilizaveis (`PathSelector`, `DevToolsToolFrame`, etc.)
- validacao de formulario com mensagem inline
- dialogos centralizados em `UiMessageService`

## 8. Testes

Projeto de testes:

- `src/Tools/DevTools.Tests`

Suites principais:

- integracao de roteamento
- simulacao de uso WPF
- persistencia SQLite
- engines de notas e backup

Resultado atual:

- 36 aprovados, 2 ignorados, 0 falhas

Ignorados:

1. `PathSelectorTests.SelectedPath_Updates_TextBox_Display`
2. `SnapshotWindowTests.ProcessButton_Persists_SelectedPath_To_Settings`

Motivo:

- afinidade de thread do `Application.Current` no host xUnit.

## 9. Build e distribuicao

Build:

```powershell
dotnet build src/DevTools.slnx -c Debug
```

Testes:

```powershell
dotnet test src/Tools/DevTools.Tests/DevTools.Tests.csproj -v minimal
```

Instalador:

```powershell
build\build_installer.bat <versao>
```

## 10. Riscos e backlog tecnico

1. remover `Skip` dos 2 testes WPF com fixture STA estavel
2. validacao E2E real de Ngrok em ambiente com binario instalado
3. promover SQLite como padrao (mantendo fallback claro)
4. consolidar modal padrao propria em toda a aplicacao
