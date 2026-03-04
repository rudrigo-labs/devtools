# DevTools.Migrations - Como usar (CLI)

## Objetivo

Executar comandos do EF Core (add migration e update database) com assistente.

## Passos

1. Selecione `Migrations`.
2. Escolha a acao (add ou update).
3. Escolha o provider.
4. Informe os caminhos e o DbContext.

## Entradas

- Acao (AddMigration ou UpdateDatabase)
- Provider (SqlServer ou Sqlite)
- Root do projeto
- Startup project (.csproj)
- DbContext (namespace completo)
- Migrations project (.csproj)
- Args adicionais (opcional)
- Nome da migration (obrigatorio para add)
- Dry-run (s/n)
- Working directory (opcional)

## Saida

- Comando completo executado
- StdOut / StdErr

## Observacoes

- Dry-run mostra o comando sem executar.
