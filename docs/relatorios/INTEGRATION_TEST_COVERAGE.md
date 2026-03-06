# DevTools - Cobertura de Testes (Integracao/Smoke)

Data de referencia: 2026-03-05

## 1. Escopo automatizado atual

| Area | Arquivo de Teste | Cobertura principal |
|---|---|---|
| Router de ferramentas | `src/Tools/DevTools.Tests/ToolRouterIntegrationTests.cs` | Resolucao por ID, desabilitacao, singleton e launch modes |
| Registry de ferramentas | `src/Tools/DevTools.Tests/ToolRegistryBehaviorTests.cs` | Filtro, ordenacao e override por ID |
| Simulacao de uso WPF | `src/Tools/DevTools.Tests/ToolUsageSimulationTests.cs` | Fluxo ponta a ponta de abertura/uso das ferramentas |
| Validacao de Google Drive na UI | `src/Tools/DevTools.Tests/MainWindowGoogleDriveValidationTests.cs` | Campos obrigatorios e comportamento do painel |
| Persistencia SQLite | `src/Tools/DevTools.Tests/SqliteStoresIntegrationTests.cs` | Settings, configurations e metadados |
| Notas (engine e backup) | `src/Tools/DevTools.Tests/NotesEngineIntegrationTests.cs`, `NotesBackupVolumeIntegrationTests.cs` | CRUD, listagem, backup/import e conflitos |
| Backend bootstrap | `src/Tools/DevTools.Tests/StorageBackendAndBootstrapperTests.cs` | Resolucao de backend e inicializacao SQLite |

## 2. Resultado de execucao mais recente

Comando:

```powershell
dotnet test src/Tools/DevTools.Tests/DevTools.Tests.csproj -v minimal
```

Resultado:

- Total: 38
- Aprovados: 36
- Ignorados: 2
- Falhas: 0

Comando da solucao:

```powershell
dotnet test src/DevTools.slnx -v minimal
```

Resultado:

- sem falhas

## 3. Testes ignorados atualmente

1. `PathSelectorTests.SelectedPath_Updates_TextBox_Display`
2. `SnapshotWindowTests.ProcessButton_Persists_SelectedPath_To_Settings`

Motivo do `Skip`:

- instabilidade de afinidade de thread WPF (`Application.Current`) no host xUnit.

## 4. Como validar manualmente os 2 cenarios ignorados

### 4.1 PathSelector

1. Abrir qualquer janela com `PathSelector` (ex.: Organizer, Migrations, Notes configuracao).
2. Selecionar uma pasta/arquivo.
3. Confirmar que o texto exibido no campo corresponde ao caminho selecionado.

### 4.2 Snapshot persiste caminho

1. Abrir Snapshot.
2. Selecionar pasta no `RootPathSelector`.
3. Executar `Gerar Snapshot`.
4. Fechar e reabrir Snapshot.
5. Confirmar que o ultimo caminho permanece carregado.

## 5. Escopo nao coberto automaticamente (ainda)

- Google Drive real (OAuth + upload real) na suite CI.
- Ngrok/SSH com processo externo real em ambiente de CI.

## 6. Observacao sobre rodada 2026-03-05

Foi feita validacao bypass ad-hoc dos 2 cenarios skipped em ambiente local, registrada em:

- `docs/RELATORIO_TESTES_BYPASS_WPF_2026-03-05.md`

