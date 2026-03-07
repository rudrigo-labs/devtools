# LOG DEMANDA - Infraestrutura Compartilhada

- Demanda relacionada: `docs/definicoes/DEFINICAO_INFRAESTRUTURA_COMPARTILHADA_20260307_0152.md`
- Status: Em andamento
- Iniciado em: 2026-03-07 01:52
- Atualizado em: 2026-03-07 02:29

## Entradas
- [2026-03-07 01:52] Demanda formalizada para analise aprofundada da Infrastructure.
- [2026-03-07 01:52] Varredura executada em `legacy-v1/src/Infrastructure/DevTools.Infrastructure`.
- [2026-03-07 01:52] Diagnostico consolidado com itens reaproveitaveis e pontos criticos.
- [2026-03-07 02:01] Implementada estrutura base da Infrastructure no projeto atual (`Persistence`, `Entities`, `Bootstrap`, `OptionsFactory`, `DesignTimeFactory`).
- [2026-03-07 02:01] Definido baseline tecnico SQLite via interceptor de conexao com PRAGMA (`WAL`, `busy_timeout`, `foreign_keys`, `synchronous`, `temp_store`).
- [2026-03-07 02:01] Removido acoplamento direto da Infrastructure com projetos de Tools no `.csproj`.
- [2026-03-07 02:02] Build validado com sucesso para `Core` e `Infrastructure`.
- [2026-03-07 02:13] Repositorio concreto da Snapshot implementado na Infrastructure (`SnapshotEntityRepository`).
- [2026-03-07 02:14] Cobertura da definicao avancou para Snapshot; pendentes mais 2 ferramentas.
- [2026-03-07 02:29] Escopo ajustado por decisao do usuario: fechar Snapshot primeiro e remover obrigatoriedade de +2 ferramentas nesta fase.

## Evidencias
- `legacy-v1/src/Infrastructure/DevTools.Infrastructure/Persistence/DevToolsDbContext.cs`
- `legacy-v1/src/Infrastructure/DevTools.Infrastructure/Persistence/SqliteBootstrapper.cs`
- `legacy-v1/src/Infrastructure/DevTools.Infrastructure/Persistence/StorageBackendResolver.cs`
- `legacy-v1/src/Infrastructure/DevTools.Infrastructure/Persistence/Stores/ToolConfigurationStoreFactory.cs`
- `legacy-v1/src/Infrastructure/DevTools.Infrastructure/Persistence/Stores/SqliteToolConfigurationStore.cs`
- `legacy-v1/src/Infrastructure/DevTools.Infrastructure/Persistence/Stores/JsonSettingsStore.cs`
- `legacy-v1/src/Infrastructure/DevTools.Infrastructure/Persistence/Stores/SqliteSettingsStore.cs`
- `src/Infrastructure/DevTools.Infrastructure/Persistence/DevToolsDbContext.cs`
- `src/Infrastructure/DevTools.Infrastructure/Persistence/SqliteBootstrapper.cs`
- `src/Infrastructure/DevTools.Infrastructure/Persistence/SqliteDbContextOptionsFactory.cs`
- `src/Infrastructure/DevTools.Infrastructure/Persistence/SqlitePragmaConnectionInterceptor.cs`
- `src/Infrastructure/DevTools.Infrastructure/Persistence/DevToolsDbContextDesignTimeFactory.cs`
- `src/Infrastructure/DevTools.Infrastructure/Persistence/Entities/ToolConfigurationEntity.cs`
- `src/Infrastructure/DevTools.Infrastructure/Persistence/Entities/AppSettingEntity.cs`
- `src/Infrastructure/DevTools.Infrastructure/DevTools.Infrastructure.csproj`
- `src/Infrastructure/DevTools.Infrastructure/Persistence/Repositories/SnapshotEntityRepository.cs`
