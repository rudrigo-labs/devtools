# LOG DEMANDA - Arquitetura de Persistencia e Orquestracao

- Demanda relacionada: `docs/definicoes/DEFINICAO_ARQUITETURA_PERSISTENCIA_ORQUESTRACAO_20260306_1856.md`
- Status: Concluida
- Responsavel: Equipe DevTools
- Iniciado em: 2026-03-06 18:56
- Atualizado em: 2026-03-06 20:39

## Entradas de log
- [2026-03-06 18:56] Definicao consolidada criada a partir de dois documentos-base, com ajustes para o contexto real do DevTools.
- [2026-03-06 18:56] `docs/architecture.md` atualizado para apontar a definicao consolidada como fonte canonica.
- [2026-03-06 19:15] Demanda formalizada com checklist por fases em documento de pendencias dedicado.
- [2026-03-06 19:20] Inventario tecnico concluido: persistencia estava centralizada em `Presentation.Wpf/Persistence` (DbContext, entities, stores, backend resolver/bootstrap).
- [2026-03-06 19:22] Projeto `src/Infrastructure/DevTools.Infrastructure` criado e adicionado a solucao.
- [2026-03-06 19:24] Bloco completo `Persistence` movido de `Presentation.Wpf` para `Infrastructure`.
- [2026-03-06 19:26] Namespaces migrados para `DevTools.Infrastructure.Persistence.*` e logging interno desacoplado via `InfraLogger`.
- [2026-03-06 19:28] Factories de infraestrutura adicionadas (`SettingsStoreFactory`, `ToolConfigurationStoreFactory`) para remover montagem de store/EF no host.
- [2026-03-06 19:30] `App.xaml.cs` e `ConfigService` refatorados para consumir infraestrutura em vez de criar persistencia local da WPF.
- [2026-03-06 19:31] `MainWindow` e testes ajustados para novo namespace de persistencia.
- [2026-03-06 19:34] `dotnet build src/DevTools.slnx -c Debug` executado com sucesso (0 erros, 0 warnings).
- [2026-03-06 19:36] `dotnet test src/Tools/DevTools.Tests/DevTools.Tests.csproj -v minimal` executado com sucesso (36 aprovados, 2 ignorados, 0 falhas).
- [2026-03-06 19:40] Fases 1-5 marcadas como concluidas e registro mestre atualizado.
- [2026-03-06 20:05] Status reaberto por decisao de arquitetura: ainda falta etapa de entidades e repositorios por ferramenta (sem conceito de repositorio central no host).
- [2026-03-06 20:22] Fase 6 (Notes) iniciada: criado repositorio de dominio `INotesItemsRepository/NotesItemsRepository` na tool Notes.
- [2026-03-06 20:22] Exclusao de nota migrada da WPF para dominio Notes com nova acao `NotesAction.DeleteItem`.
- [2026-03-06 20:22] Validacao tecnica apos rodada Notes: build e testes verdes (`36 aprovados`, `2 ignorados`).
- [2026-03-06 20:29] Metadados Notes alinhados ao dominio: `JsonNoteMetadataStore` e `SqliteNoteMetadataStore` passaram a usar `INoteMetadataRepository` e `NoteMetadataEntity`.
- [2026-03-06 20:29] Tipos legados removidos da infraestrutura (`INoteMetadataStore`, `NoteMetadataRecord`).
- [2026-03-06 20:29] `DevTools.Infrastructure` passou a referenciar `DevTools.Notes` para contrato/entidade de dominio.
- [2026-03-06 20:29] Validacao tecnica da rodada atual: `dotnet build src/DevTools.slnx -c Debug` e `dotnet test src/Tools/DevTools.Tests/DevTools.Tests.csproj -v minimal` com sucesso.
- [2026-03-06 20:34] Configuracoes nomeadas ganharam repositorio de dominio dedicado no Core (`IToolConfigurationRepository`, `ToolConfigurationRepository`).
- [2026-03-06 20:34] `ToolConfigurationManager` foi desacoplado de `IToolConfigurationStore` direto e passou a operar via repositorio de dominio.
- [2026-03-06 20:34] Validacao tecnica: build verde; primeira execucao de testes teve falha intermitente do `testhost` (CLR assert), segunda execucao concluiu com sucesso (`36 aprovados`, `2 ignorados`).
- [2026-03-06 20:38] Teste de integracao adicionado para fluxo `Manager -> Repository -> SqliteStore` (`ToolConfigurationManager_WithRepository_UsesInfrastructureStore`).
- [2026-03-06 20:38] Validacao final da rodada: `dotnet test src/Tools/DevTools.Tests/DevTools.Tests.csproj -v minimal` concluido com sucesso (`37 aprovados`, `2 ignorados`, total `39`).
- [2026-03-06 20:39] Checklist da demanda fechado e status atualizado para concluido no controle mestre.

## Mudancas de escopo
- [2026-03-06 19:18] Escopo mantido: migracao estrutural completa da persistencia da WPF para Infrastructure no mesmo ciclo.
- [2026-03-06 20:05] Escopo estendido: incluir etapa explicita para dominio por ferramenta (`Entities`/`Repositories` em `Tools`), mantendo Infrastructure como detalhe tecnico.

## Evidencias
- Definicao: `docs/definicoes/DEFINICAO_ARQUITETURA_PERSISTENCIA_ORQUESTRACAO_20260306_1856.md`
- Pendencias: `docs/pendencias/PENDENCIAS_ARQUITETURA_PERSISTENCIA_ORQUESTRACAO_20260306_1856.md`
- Projeto novo: `src/Infrastructure/DevTools.Infrastructure/DevTools.Infrastructure.csproj`
- Persistencia movida: `src/Infrastructure/DevTools.Infrastructure/Persistence/*`
- Host atualizado: `src/Presentation/DevTools.Presentation.Wpf/App.xaml.cs`, `src/Presentation/DevTools.Presentation.Wpf/Services/ConfigService.cs`
- Notes (repositorio + delete no dominio): `src/Tools/DevTools.Notes/Repositories/*`, `src/Tools/DevTools.Notes/Engine/NotesEngine.cs`, `src/Presentation/DevTools.Presentation.Wpf/Views/NotesWindow.xaml.cs`
- Solucao atualizada: `src/DevTools.slnx`
- Build: `dotnet build src/DevTools.slnx -c Debug`
- Testes: `dotnet test src/Tools/DevTools.Tests/DevTools.Tests.csproj -v minimal`

## Proximos passos
1. Implementar fase de dominio por ferramenta (entidades + repositorios nas tools).
2. Validacao manual funcional no host WPF (troca de backend JSON/SQLite e telas de configuracao).
3. Criar migracao de banco versionada (alem do `EnsureCreated`) em fase dedicada.

