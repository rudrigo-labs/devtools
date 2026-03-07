# DEFINICAO - Estrutura Padrao de Tool

- Status: Em andamento
- Criado em: 2026-03-07 01:17
- Objetivo: Padronizar a estrutura interna de todas as tools do DevTools.

## Estrutura padrao
Cada tool deve seguir o padrao:

`src/Tools/DevTools.<ToolName>/`

1. `Engine/`
- `<ToolName>Engine.cs`

2. `Validators/`
- `<ToolName>RequestValidator.cs`

3. `Models/`
- `<ToolName>Request.cs`
- `<ToolName>Result.cs`
- modelos auxiliares especificos da tool

4. `Repositories/`
- `I<ToolName>Repository.cs`

5. `Services/` (opcional)
- servicos auxiliares especificos

## Regras obrigatorias
1. Toda tool executa pela Engine (`ExecuteAsync`).
2. Toda tool possui `Request` e `Result`.
3. Toda tool possui `Repository` por interface.
4. Toda tool persiste dados da configuracao nomeada base:
- `Id` (slug)
- `Name`
- `Description`
- `IsActive`
- `CreatedAtUtc`
- `UpdatedAtUtc`
5. Tool nao referencia WPF.
6. Tool nao acessa `DbContext` direto.
7. Implementacao concreta de repositorio fica em `Infrastructure`.

## Responsabilidade por camada
1. Host:
- coleta input
- monta request
- chama engine
- exibe result

2. Tool:
- validacao
- regra de negocio
- orquestracao
- dependencia de repositorio por interface

3. Infrastructure:
- repositorios concretos
- acesso a DbContext/SQLite

## Beneficios
- previsibilidade
- manutencao simplificada
- consistencia entre tools
- melhor cobertura de testes
