# DEFINICAO - Core Compartilhado

- Status: Consolidada
- Criado em: 2026-03-07 01:04
- Objetivo: Definir primeiro tudo que e compartilhado antes de implementar ajustes por ferramenta.

## Escopo
1. Definir contratos e tipos base do `Core`.
2. Remover ambiguidade sobre o que fica no `Core` e o que fica em `Tools`.
3. Estabelecer bloqueadores para execucao da Snapshot piloto.

## Itens compartilhados (alvo)
1. Entidade base canonica:
- `Id` (slug canonico)
- `Name`
- `Description`
- `IsActive`
- `CreatedAtUtc`
- `UpdatedAtUtc`
2. Contratos compartilhados:
- resultados padrao (`Result`, `Error`)
- contratos de validacao
- contratos de repositorio generico (somente quando fizer sentido)
3. Utilitarios compartilhados:
- relogio/tempo UTC
- normalizacao de slug

## Politica de repositorio compartilhado
1. `IRepository<T>` no Core e contrato opcional para entidades de configuracao nomeada.
2. Usar repositorio generico apenas para CRUD simples de entidade baseada em `Id` (slug).
3. Quando houver regra de consulta especializada por ferramenta, criar interface especifica na Tool e implementar na Infrastructure.
4. Core nao contem implementacao concreta de repositorio.

## Politica de persistencia
1. Nao usar persistencia de configuracao em arquivo para ferramentas.
2. Arquivo fica restrito a artefatos tecnicos de sistema/ambiente quando necessario.
3. Persistencia funcional de ferramentas ocorre via Infrastructure e banco.

## Regras
1. `Core` nao conhece WPF nem infraestrutura concreta.
2. `Core` contem apenas o que e comum entre ferramentas.
3. Regra especifica de ferramenta permanece em `Tools`.

## Dependencia de execucao
1. Snapshot Fase 2 (ajuste de modelos/repositorios) so comeca apos esta definicao.
