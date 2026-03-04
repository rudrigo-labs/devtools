# Auditoria de Documentacao - 2026-03-04

## Objetivo

Revisar a pasta `docs/`, identificar conteudo obsoleto, consolidar duplicacoes e definir uma estrutura canonica de manutencao.

## Resultado geral

- Estrutura canonica definida:
  - `docs/` para guias ativos
  - `docs/tools/` para documentacao por ferramenta
  - `docs/_obsolete/` para historico legado
- Documento de migracao IDE-style movido para a raiz de `docs`.
- Duplicacoes de guias por ferramenta removidas da raiz e consolidadas em `docs/tools/`.

## Arquivos movidos para obsoletos

### Contexto temporario
- `docs/contexto-atual.md` -> `docs/_obsolete/legacy_context/contexto-atual.md`
- `docs/contexto-template.md` -> `docs/_obsolete/legacy_context/contexto-template.md`
- `docs/temp.txt` -> `docs/_obsolete/legacy_context/temp.txt`

### Planos antigos
- `docs/IMPLEMENTATION_PLAN.md` -> `docs/_obsolete/legacy_plans/IMPLEMENTATION_PLAN.md`
- `docs/IMPLEMENTATION_PLAN_ISOLATED.md` -> `docs/_obsolete/legacy_plans/IMPLEMENTATION_PLAN_ISOLATED.md`

### Guias legados
- `docs/UserGuide.md` -> `docs/_obsolete/legacy_guides/UserGuide.md`

### Guias antigos por ferramenta (consolidados em `docs/tools/`)
- `docs/tool-harvest.md` -> `docs/_obsolete/legacy_tool_docs/tool-harvest.md`
- `docs/tool-imagesplit.md` -> `docs/_obsolete/legacy_tool_docs/tool-imagesplit.md`
- `docs/tool-migrations.md` -> `docs/_obsolete/legacy_tool_docs/tool-migrations.md`
- `docs/tool-ngrok.md` -> `docs/_obsolete/legacy_tool_docs/tool-ngrok.md`
- `docs/tool-notes.md` -> `docs/_obsolete/legacy_tool_docs/tool-notes.md`
- `docs/tool-organizer.md` -> `docs/_obsolete/legacy_tool_docs/tool-organizer.md`
- `docs/tool-rename.md` -> `docs/_obsolete/legacy_tool_docs/tool-rename.md`
- `docs/tool-searchtext.md` -> `docs/_obsolete/legacy_tool_docs/tool-searchtext.md`
- `docs/tool-snapshot.md` -> `docs/_obsolete/legacy_tool_docs/tool-snapshot.md`
- `docs/tool-sshtunnel.md` -> `docs/_obsolete/legacy_tool_docs/tool-sshtunnel.md`
- `docs/tool-utf8convert.md` -> `docs/_obsolete/legacy_tool_docs/tool-utf8convert.md`

## Arquivos atualizados/consolidados

- `docs/tools-overview.md` (agora referencia `docs/tools/` como fonte canonica)
- `docs/tools/index.md` (indice atualizado e nota de consolidacao)
- `docs/README.md` (mapa oficial da documentacao)
- `docs/_obsolete/README.md` (politica e organizacao do legado)

## Arquivo movido para local correto

- `src/Presentation/DevTools.Presentation.Wpf/docs/IDE_STYLE_MIGRATION_PLAN.md`
  -> `docs/IDE_STYLE_MIGRATION_PLAN.md`

## Regras definidas

1. Novos documentos devem ser criados em `docs/` (na raiz do repositorio).
2. Documentacao por ferramenta fica em `docs/tools/`.
3. Conteudo substituido vai para `docs/_obsolete/` (nao e apagado diretamente).
