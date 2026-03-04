# DevTools - Cobertura de Testes (Integracao/Smoke)

## Escopo Atual

Esta matriz resume o que esta automatizado sem depender de servicos externos (Google Drive fora de escopo).

| Area | Arquivo de Teste | Cobertura principal |
|---|---|---|
| Router de ferramentas | `src/Tools/DevTools.Tests/ToolRouterIntegrationTests.cs` | Resolucao por ID, tool desabilitada, singleton/non-singleton, launch background |
| Registry de ferramentas | `src/Tools/DevTools.Tests/ToolRegistryBehaviorTests.cs` | Filtro de tools desabilitadas, ordenacao por categoria/ordem/titulo, override por mesmo ID |
| Simulacao de uso WPF | `src/Tools/DevTools.Tests/ToolUsageSimulationTests.cs` | Abertura de ferramentas, fluxos principais (Organizer, Harvest, Search, Rename, Snapshot, Utf8, Image, Migrations, Notes, janelas auxiliares) |
| Validacao de Google Drive na UI | `src/Tools/DevTools.Tests/MainWindowGoogleDriveValidationTests.cs` | Campos obrigatorios no painel de configuracao (sem teste de API externa) |
| Persistencia SQLite (settings) | `src/Tools/DevTools.Tests/SqliteStoresIntegrationTests.cs` | Save/Get de seções, defaults e idempotencia |
| Persistencia SQLite (profiles) | `src/Tools/DevTools.Tests/SqliteStoresIntegrationTests.cs` | Save/Load com ordenacao e substituicao completa |
| Persistencia SQLite (note metadata) | `src/Tools/DevTools.Tests/SqliteStoresIntegrationTests.cs` | Upsert/Get/Delete de metadados |
| Engine de notas (filesystem) | `src/Tools/DevTools.Tests/NotesEngineIntegrationTests.cs` | Criacao/listagem/leitura com `.md` e `.txt` |
| Engine de notas (edicao) | `src/Tools/DevTools.Tests/NotesEngineIntegrationTests.cs` | Edicao de nota existente (SaveNote/LoadNote) com sincronizacao de indice |
| Backup de notas em volume | `src/Tools/DevTools.Tests/NotesBackupVolumeIntegrationTests.cs` | Export/Import ZIP com lote grande e validacao de conflitos |
| Startup SQLite | `src/Tools/DevTools.Tests/StorageBackendAndBootstrapperTests.cs` | Resolucao de backend por env var e bootstrap idempotente do banco |

## Cobertura Nao Incluida

- Google Drive (autenticacao e upload reais) por decisao do projeto.
- Integracao real de SSH/ngrok com processos externos em ambiente de CI.
