# REGRAS ESPECIFICAS DO PROJETO - DEVTOOLS

Estas regras descrevem a estrutura oficial do projeto DevTools.

Se houver conflito com AGENT_EXECUTION_RULES.md, estas regras tem prioridade.

## Estrutura da solucao

Todos os projetos devem estar dentro de:

`src/`

Organizacao atual:

- `src/Hosts/` (host da interface)
- `src/Core/` (contratos e tipos compartilhados)
- `src/Tools/` (bibliotecas de cada ferramenta)
- `src/Infrastructure/` (implementacoes tecnicas, persistencia e integracoes)

## Regra de arquitetura

- O Host WPF orquestra e chama as ferramentas.
- Logica de dominio deve ficar nas bibliotecas de ferramenta.
- Host/UI nao deve virar dominio.
- Fluxo oficial: `Host -> Tool -> Infrastructure -> Database`.
- Escopo atual: sem CLI nesta fase.

## Regra de frameworks

- Novos projetos .NET devem usar `net10.0`.
- Mudanca de framework so com instrucao explicita.

## Regra de pasta/projeto

- Novo projeto deve ser criado dentro de pasta com o mesmo nome do projeto.

## Regras de documentacao locais

- Seguir as regras globais de `.ai/DOCUMENTATION_RULES.md`.
- Seguir tambem as politicas locais em `docs/controle/`.
