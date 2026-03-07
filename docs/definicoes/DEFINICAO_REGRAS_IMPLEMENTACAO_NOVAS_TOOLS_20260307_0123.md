# DEFINICAO - Regras de Implementacao para Novas Tools

- Status: Em andamento
- Criado em: 2026-03-07 01:23
- Objetivo: Definir regras obrigatorias para implementacao consistente de novas tools.

## Regra 1 - Local da implementacao
Toda nova tool deve ser implementada em:
`src/Tools/DevTools.<ToolName>/`

A logica da funcionalidade nao deve ser implementada em Host/UI.

## Regra 2 - Camadas obrigatorias
Fluxo obrigatorio:
`Host (WPF) -> Tool -> Infrastructure -> Database`

## Regra 3 - Dependencias proibidas
- `Tool -> WPF`
- `Host -> DbContext`
- `Infrastructure -> Host`

## Regra 4 - Estrutura minima por tool
Cada tool deve conter, no minimo:
- `Engine`
- `Validators`
- `Models`
- `Repositories`

`Services` e opcional.

## Regra 5 - Padrao de execucao
Padrao obrigatorio:
`Request -> Engine.ExecuteAsync(request, cancellationToken) -> Result`

A UI nunca executa logica da tool diretamente.

## Regra 6 - Persistencia obrigatoria por repositorio
Toda tool depende de interface de repositorio:
`I<ToolName>Repository`

A implementacao concreta fica em `Infrastructure`.

Justificativa:
toda tool persiste configuracao nomeada base (`Id`, `Name`, `Description`, `IsActive`, datas).

## Regra 7 - Independencia de interface
Tool deve ser reutilizavel em:
- Host WPF
- testes automatizados
- APIs futuras

## Regra 8 - Responsabilidade da Engine
A Engine e o ponto unico de execucao da tool:
- recebe request
- valida
- aplica regra de negocio
- chama repositorio
- retorna result

## Regra 9 - Observacao de fase atual
- Fase atual: Host WPF (CLI fora de escopo nesta etapa).
- Antes de execucao por tool, concluir definicoes do Core compartilhado.
