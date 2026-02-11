# DevTools.SearchText - Como usar (CLI)

## Objetivo

Buscar texto ou regex dentro de arquivos de uma pasta.

## Passos

1. Selecione `Search Text`.
2. Informe pasta raiz e o texto/regex.
3. Configure filtros e limites.

## Entradas

- Pasta raiz (obrigatorio)
- Texto ou regex (obrigatorio)
- Usar regex (s/n)
- Case sensitive (s/n)
- Palavra inteira (s/n)
- Includes (globs) (opcional)
- Excludes (globs) (opcional)
- Max KB por arquivo (opcional)
- Ignorar binarios (s/n)
- Max matches por arquivo (opcional)
- Mostrar linhas (s/n)

## Saida

- Total de arquivos escaneados
- Total de arquivos com match
- Total de ocorrencias
- Lista de resultados (opcional)

## Observacoes

- Exemplo de glob: `src/**/*.cs`.
