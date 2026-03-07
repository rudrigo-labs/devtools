# DEFINICAO - Fluxo de Processo da Snapshot

- Status: Em andamento
- Criado em: 2026-03-07 02:33
- Objetivo: Documentar o fluxo completo da Snapshot para consulta rapida durante implementacao e testes.

## Escopo desta definicao
1. Descrever o que ja foi implementado na Snapshot.
2. Descrever o fluxo de configuracao (`SnapshotEntity`) ate persistencia em banco.
3. Descrever o fluxo de execucao da ferramenta via `SnapshotEngine`.
4. Registrar o que ainda falta para fechar fim-a-fim.

## Componentes atuais da Snapshot
1. Entidade:
- `src/Tools/DevTools.Snapshot/Models/SnapshotEntity.cs`

2. Validacao:
- `src/Tools/DevTools.Snapshot/Validation/SnapshotEntityValidator.cs`
- `src/Tools/DevTools.Snapshot/Validation/SnapshotExecutionRequestValidator.cs`

3. Servico de entidade:
- `src/Tools/DevTools.Snapshot/Services/SnapshotEntityService.cs`

4. Contrato de repositorio:
- `src/Tools/DevTools.Snapshot/Repositories/ISnapshotEntityRepository.cs`

5. Repositorio concreto (Infrastructure):
- `src/Infrastructure/DevTools.Infrastructure/Persistence/Repositories/SnapshotEntityRepository.cs`

6. Execucao:
- `src/Tools/DevTools.Snapshot/Engine/SnapshotEngine.cs`

## Fluxo A - Configuracao da ferramenta (CRUD de SnapshotEntity)
1. Host recebe dados da tela de Snapshot.
2. Host monta `SnapshotEntity`.
3. Host chama `SnapshotEntityService.UpsertAsync`.
4. Service garante identidade:
- gera `Id` slug a partir de `Name` quando necessario;
- seta `CreatedAtUtc`/`UpdatedAtUtc`.
5. Service executa `SnapshotEntityValidator`.
6. Com validacao ok, Service chama `ISnapshotEntityRepository.UpsertAsync`.
7. Repository grava em `tool_configurations`:
- metadados no corpo da tabela (`id`, `name`, `description`, `is_active`, `is_default`, datas);
- dados especificos de Snapshot em `payload_json`.
8. Banco SQLite persiste via `DevToolsDbContext`.

## Fluxo B - Selecao de padrao (default)
1. Host/Service chama `SetDefaultAsync(id)`.
2. Repository abre transacao.
3. Desmarca padrao anterior da Snapshot.
4. Marca novo `id` como default.
5. Commit.

## Fluxo C - Execucao da Snapshot
1. Host coleta parametros de execucao e monta `SnapshotExecutionRequest`.
2. Host chama `SnapshotEngine.ExecuteAsync`.
3. Engine valida `SnapshotExecutionRequest`.
4. Em caso de erro: retorna `RunResult.Fail`.
5. Em caso de sucesso:
- publica progresso (`IProgressReporter`);
- executa processamento da Snapshot;
- retorna `RunResult.Success(SnapshotExecutionResult)`.

## Estado atual
1. Fluxo de modelo/contrato/repositorio da Snapshot: implementado.
2. Fluxo de engine com validacao e resultado padrao: implementado.
3. Integracao completa do Host com esse fluxo: pendente.
4. Migracao final do banco no ambiente principal: pendente por decisao de executar no fechamento da integracao do Host.

## Regra operacional desta frente
1. Fechar Snapshot fim-a-fim primeiro.
2. So depois replicar padrao para outras ferramentas.

