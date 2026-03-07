# DEFINICAO - Infraestrutura Compartilhada

- Status: Em andamento
- Criado em: 2026-03-07 01:52
- Objetivo: Definir a base canonica da Infrastructure para a nova fase, sem repetir problemas do legado.

## Escopo
1. Definir fronteira da Infrastructure na arquitetura oficial.
2. Definir padrao de persistencia no banco (sem fallback silencioso para JSON em dados de ferramenta).
3. Definir padrao tecnico de SQLite (bootstrap, lock, concorrencia, migracoes e observabilidade).
4. Definir politica de repositorios concretos por ferramenta.

## Diagnostico detalhado do legado (`legacy-v1/src/Infrastructure/DevTools.Infrastructure`)
1. Pontos reaproveitaveis:
- `DevToolsDbContext` como concentrador de tabelas.
- Stores SQLite por responsabilidade (`SqliteSettingsStore`, `SqliteToolConfigurationStore`, `SqliteNoteMetadataStore`).
- `SqlitePathProvider` como abstracao de caminho e connection string.

2. Pontos criticos (nao canonicos para a nova fase):
- `EnsureCreated()` em vez de migracoes versionadas.
- fallback silencioso SQLite -> JSON em factories.
- `StorageBackendResolver` com default em JSON.
- mistura de configuracao funcional de ferramenta em JSON store.
- tratamento de erro que engole excecao e segue com default em varios pontos.
- acoplamento a contratos legados de configuracao (`DevTools.Core.Configuration`).
- ausencia de padrao explicito de lock/concorrencia no SQLite (WAL, busy timeout, foreign_keys).

## Diretrizes canonicas da nova Infrastructure
1. Infra implementa contratos, nao define dominio.
2. Persistencia funcional de ferramenta: banco.
3. JSON somente para artefatos tecnicos de sistema/ambiente, nunca como fallback oculto de dados funcionais.
4. Erro de persistencia critica deve propagar contexto suficiente para o host (sem mascarar falha).
5. SQLite deve iniciar com baseline tecnico controlado (PRAGMA e politicas de conexao).
6. Evolucao de schema por migracoes, nao por `EnsureCreated` como mecanismo principal.

## Dependencias
1. Esta definicao depende do Core compartilhado ja consolidado.
2. Snapshot e demais ferramentas passam a consumir repositorios concretos apenas apos esta base.

## Estado de execucao (fase atual)
1. Estrutura base implementada no projeto atual com:
- `DevToolsDbContext` canonico;
- entidades de infraestrutura (`AppSettingEntity`, `ToolConfigurationEntity`);
- `SqlitePathProvider`;
- `SqliteDbContextOptionsFactory`;
- `SqlitePragmaConnectionInterceptor`;
- `SqliteBootstrapper`;
- `DevToolsDbContextDesignTimeFactory`.
2. Fase pendente: concluir fechamento fim-a-fim da Snapshot no host e validacao final.
3. Proxima etapa (fora desta fase): replicar o padrao para outras ferramentas, uma por vez.
