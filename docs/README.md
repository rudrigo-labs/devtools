# Documentacao DevTools

Este diretorio contem guias tecnicos e operacionais do projeto.

## Documentos oficiais

- Manual oficial do produto: `../MANUAL.md`
- README oficial do repositorio: `../README.md`
- Cobertura de testes: `INTEGRATION_TEST_COVERAGE.md`
- Relatorio oficial de fechamento da versao: `RELATORIO_FECHAMENTO_VERSAO_2026-03-04.md`
- Relatorio de testes e varredura: `RELATORIO_TESTES_E_VARREDURA_GERAL_2026-03-04.md`
- Guia de distribuicao: `GUIA_DE_DISTRIBUICAO.md`
- Plano de migracao SQLite: `SQLITE_MIGRATION_PLAN.md`

## Estrutura

- `docs/_obsolete/`: documentos legados, historicos ou substituidos
- raiz de `docs/`: documentos transversais ativos

## Regras de manutencao

1. Evitar duplicidade entre documentos oficiais.
2. Quando um documento ficar legado, mover para `docs/_obsolete/`.
3. Para documentacao de usuario final, priorizar `MANUAL.md` na raiz.
4. Relatorios de fechamento de versao devem registrar build/testes e pendencias tecnicas.
