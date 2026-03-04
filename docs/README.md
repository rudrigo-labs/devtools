# Documentacao DevTools

Este diretorio e a fonte oficial de documentacao do projeto.

## Estrutura

- `docs/tools/`: documentacao por ferramenta (fonte canonica).
- `docs/_obsolete/`: documentos legados, substituidos ou descontinuados.
- Arquivos na raiz de `docs/`: guias transversais (arquitetura, configuracao, distribuicao, planos ativos).

## Guias principais

- `MANUAL_DO_USUARIO.md`
- `CONFIGURATION.md`
- `SETTINGS_AND_PROFILES_GUIDE.md`
- `TechnicalDoc.md`
- `GUIA_DE_DISTRIBUICAO.md`
- `IDE_STYLE_MIGRATION_PLAN.md`
- `SQLITE_MIGRATION_PLAN.md`
- `tools-overview.md`
- `DOCS_AUDIT_2026-03-04.md`

## Regras de manutencao

1. Evitar duplicacao de conteudo por ferramenta fora de `docs/tools/`.
2. Quando um documento for substituido, mover para `docs/_obsolete/` com contexto.
3. Criar novos documentos sempre em `docs/` (raiz ou subpastas), nao em `src/`.
