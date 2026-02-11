# DevTools.Rename - Como usar (CLI)

## Objetivo

Renomear arquivos/pastas e substituir texto com seguranca (identificadores C#).

## Passos

1. Selecione `Rename`.
2. Informe pasta raiz, texto antigo e novo.
3. Escolha o modo (geral ou namespace-only).
4. Configure dry-run e backup.

## Entradas

- Pasta raiz (obrigatorio)
- Texto antigo (obrigatorio)
- Texto novo (obrigatorio)
- Modo (Geral ou NamespaceOnly)
- Dry-run (s/n)
- Criar backup (s/n)
- Gerar undo log (s/n)
- Includes/Excludes (globs) (opcional)

## Saida

- Resumo com quantidades
- Caminho do relatorio e undo (quando disponivel)

## Observacoes

- Use dry-run primeiro para validar o impacto.
