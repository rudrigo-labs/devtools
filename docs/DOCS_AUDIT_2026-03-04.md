# Auditoria de Documentacao - 2026-03-04 (Atualizada)

## Objetivo

Revalidar documentos ativos e separar conteudo oficial de historico legado.

## Estrutura oficial atual

- `README.md` (raiz): resumo oficial do projeto para repositorio
- `MANUAL.md` (raiz): manual oficial de uso
- `docs/README.md`: mapa da documentacao tecnica
- `docs/_obsolete/`: historico legado e material fora de uso

## Resultado da auditoria

- Documentacao oficial atualizada para refletir estado real da aplicacao:
  - WPF como interface oficial
  - CLI obsoleto
  - suporte a storage JSON/SQLite
  - fluxo de notas local + sync opcional para Google Drive
- Cobertura de testes documentada com resultado real mais recente.
- Relatorio de fechamento de versao adicionado em `docs/RELATORIO_FECHAMENTO_VERSAO_2026-03-04.md`.

## Observacoes

- Arquivos duplicados de compatibilidade (`README (2).md`, `MANUAL (2).md`) permanecem como ponte para links antigos.
- Conteudo historico permanece em `docs/_obsolete/` para rastreabilidade.

## Regras de manutencao

1. Documentacao oficial de usuario fica em `MANUAL.md`.
2. Resumo institucional do projeto fica em `README.md`.
3. Relatorios tecnicos e planos ficam em `docs/`.
4. Qualquer material substituido deve ser movido para `docs/_obsolete/`.
