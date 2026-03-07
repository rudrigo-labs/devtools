# DEFINICAO - Ciclo de Vida de uma Tool

- Status: Em andamento
- Criado em: 2026-03-07 01:25
- Objetivo: Definir o ciclo completo de uma tool, da criacao a execucao e persistencia.

## Fases do ciclo de vida
1. Criacao da tool
2. Registro da tool no Host (WPF)
3. Execucao da tool
4. Persistencia de dados

## 1) Criacao da tool
A tool nasce em:
`src/Tools/DevTools.<ToolName>/`

Estrutura minima obrigatoria:
- `Engine`
- `Validators`
- `Models`
- `Repositories`

## 2) Registro da tool
O Host WPF registra e consome a tool:
- instancia engine/servicos
- monta requests
- recebe e exibe results

Regra:
`Host -> Tool` (nunca o contrario)

## 3) Execucao da tool
Fluxo obrigatorio:
1. Host coleta dados
2. Host monta request
3. Host chama `Engine.ExecuteAsync(request, cancellationToken)`
4. Validator valida entrada
5. Engine aplica regra de negocio
6. Engine chama repositorio por interface
7. Infrastructure acessa DbContext/SQLite
8. Engine retorna result estruturado
9. Host exibe resultado

## 4) Persistencia
Regras:
- Tool nao acessa `DbContext` diretamente.
- Tool depende de `I<ToolName>Repository`.
- Infrastructure implementa repositorio concreto.
- Banco unico: SQLite.

## Responsabilidades
1. Host (WPF)
- UI, input, request, chamada da engine, exibicao de result.

2. Tool
- validacao, regra de negocio, orquestracao, contrato de repositorio.

3. Infrastructure
- repositorios concretos e acesso ao DbContext.

4. Database
- persistencia unificada do dominio.

## Regras fundamentais
Dependencias permitidas:
`Host -> Tool -> Infrastructure -> Database`

Dependencias proibidas:
- `Tool -> WPF`
- `Infrastructure -> Host`
- `Host -> DbContext`

## Observacoes de fase atual
- Host atual: WPF (sem CLI nesta etapa).
- Core compartilhado e pre-requisito antes de replicacao nas tools.
