# Plano de Migracao para SQLite (DevTools WPF)

## Objetivo

Substituir persistencia baseada em JSON para configuracoes/metadados por SQLite, mantendo as notas `.txt/.md` como arquivos fisicos.

## Status atual (2026-03-04)

- [x] Fase 1 implementada no codigo:
  - Pacotes EF Core SQLite adicionados no projeto WPF.
  - `DevToolsDbContext` + entidades iniciais criados.
  - `SqlitePathProvider` definido para `%AppData%/DevTools/devtools.db`.
  - Bootstrap no `App.xaml.cs` para criar schema automaticamente na inicializacao.
- [x] Fase 2 implementada no codigo (abstracao + feature flag):
  - `ISettingsStore` com `JsonSettingsStore` e `SqliteSettingsStore`.
  - `IProfileStore` no Core com `JsonFileProfileStore` e `SqliteProfileStore` no WPF.
  - `INoteMetadataStore` com implementacoes JSON/SQLite.
  - Selecao de backend por feature flag: `DEVTOOLS_STORAGE_BACKEND=json|sqlite`.
- [ ] Fases 3-5 pendentes (migracao de dados JSON -> SQLite, troca oficial de runtime em todas as rotas e endurecimento).

### Como ativar SQLite agora

- PowerShell (sessao atual):
  - `$env:DEVTOOLS_STORAGE_BACKEND='sqlite'`
- Voltar para JSON:
  - `$env:DEVTOOLS_STORAGE_BACKEND='json'`
- Sem variavel definida:
  - backend padrao = `json`.

## Escopo

### Entra na migracao
- Configuracoes globais da aplicacao.
- Configuracoes por ferramenta.
- Perfis de ferramentas.
- Metadados das notas (sem mover o conteudo dos arquivos de nota para banco).
- Credenciais de integracoes (com protecao local).

### Nao entra na migracao (agora)
- Conteudo das notas (`.txt/.md`) continua no filesystem.
- Mudanca de engine para MSSQL.
- Sincronizacao colaborativa multiusuario.

## Decisao arquitetural

- Banco local: SQLite (arquivo unico em `%AppData%/DevTools/devtools.db`).
- Acesso: camada de repositorio + interfaces (`ISettingsStore`, `IProfileStore`, `INoteMetadataStore`).
- ORM recomendado: EF Core SQLite (com migracoes versionadas).

## Modelo de dados inicial

### Tabelas
1. `app_settings`
- `key` (PK, text)
- `value` (text/json)
- `updated_at` (datetime)

2. `tool_profiles`
- `id` (PK)
- `tool_key` (text, index)
- `name` (text)
- `is_default` (bool)
- `options_json` (text/json)
- `created_at`, `updated_at`

3. `notes_settings`
- `id` (PK fixo = 1)
- `storage_path` (text)
- `default_format` (text: `.txt`/`.md`)
- `auto_cloud_sync` (bool)
- `updated_at` (datetime)

4. `google_drive_settings`
- `id` (PK fixo = 1)
- `is_enabled` (bool)
- `client_id` (text)
- `project_id` (text)
- `client_secret_protected` (blob/text protegido por DPAPI)
- `folder_name` (text)
- `updated_at` (datetime)

5. `note_index` (metadados de arquivos fisicos)
- `note_key` (PK, caminho relativo/nome)
- `title` (text)
- `extension` (text)
- `last_local_write_utc` (datetime)
- `last_cloud_sync_utc` (datetime, null)
- `last_cloud_status` (text, null)
- `hash` (text, null)

## Estrategia de migracao

## Fase 1 - Infraestrutura
- Adicionar pacote `Microsoft.EntityFrameworkCore.Sqlite`.
- Criar `DevToolsDbContext` + entidades + migracao inicial.
- Criar `SqlitePathProvider` para resolver caminho do banco no AppData.

**Criterio de aceite**: app inicia, cria banco automaticamente e aplica migracoes sem erro.

## Fase 2 - Camada de abstracao
- Introduzir interfaces:
  - `ISettingsStore`
  - `IProfileStore`
  - `INoteMetadataStore`
- Implementar `JsonSettingsStore` (legado) e `SqliteSettingsStore` (novo).
- Configurar feature flag local para trocar backend sem quebrar fluxo atual.

**Criterio de aceite**: backend SQLite pode ser ligado/desligado sem regressao funcional.

## Fase 3 - Migracao de dados (bootstrap)
- No startup:
  1. Detectar se banco vazio.
  2. Ler fontes JSON legadas.
  3. Popular SQLite.
  4. Gravar marcador `migration_completed=true`.
- Manter fallback read-only nos JSON por 1 versao.

**Criterio de aceite**: usuario existente sobe sem perder configuracao/perfis.

## Fase 4 - Troca oficial do runtime
- `ConfigService` e `ProfileManager` passam a usar SQLite por padrao.
- Fluxo de notas continua escrevendo arquivos fisicos.
- `note_index` atualizado em cada salvar/sincronizar.

**Criterio de aceite**: salvar nota local e sync Google Drive continuam funcionando como hoje.

## Fase 5 - Endurecimento e limpeza
- Proteger `client_secret` via DPAPI no Windows.
- Remover escrita ativa em JSON (deixar export opcional).
- Criar comando de diagnostico/repair do banco.

**Criterio de aceite**: credenciais nao ficam mais em texto puro e operacao e estavel.

## Compatibilidade e rollback

- Rollback simples: voltar para store legado via flag.
- Manter export de configuracoes para JSON durante periodo de transicao.
- Backup automatico do `appsettings.json` antes da primeira migracao.

## Testes recomendados (quando retomar trilha de testes)

1. Migracao bootstrap com dados reais de usuario.
2. CRUD completo de perfis por ferramenta.
3. Salvamento de nota local + atualizacao do `note_index`.
4. Teste de sincronizacao Google Drive com credenciais validas.
5. Reabertura do app validando persistencia no SQLite.

## Riscos e mitigacoes

- Risco: schema inicial incompleto.
  - Mitigacao: migracoes pequenas e frequentes.
- Risco: corrupcao do banco local.
  - Mitigacao: backup de banco + comando repair.
- Risco: regressao em fluxo antigo.
  - Mitigacao: feature flag + fallback para legado por uma versao.

## Entrega minima recomendada (MVP)

- Fase 1 + Fase 2 + Fase 3 + Fase 4 para:
  - `settings`
  - `profiles`
  - `notes_settings`
  - `google_drive_settings`

Com isso, o projeto para de depender de JSON como fonte principal, sem mexer no formato fisico das notas.
