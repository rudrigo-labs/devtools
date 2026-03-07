# DEFINICAO - Fluxo Arquitetural Oficial DevTools

- Status: Em andamento
- Criado em: 2026-03-07 01:12
- Objetivo: Consolidar o fluxo arquitetural oficial para todas as ferramentas.

## Objetivo
Definir um fluxo unico para o DevTools, garantindo:
- separacao clara de responsabilidades,
- baixo acoplamento entre camadas,
- consistencia de implementacao,
- reutilizacao de ferramentas.

## Fluxo oficial
User Input

-> Host (WPF)

-> Request

-> Tool.Engine.Execute()

-> Validator

-> Repository Interface

-> Infrastructure Repository

-> DbContext

-> SQLite

-> Response / Result

-> Host (WPF)

-> UI

## Camadas
1. Host (WPF)
- Coleta input do usuario.
- Monta o request.
- Chama a tool.
- Exibe o result.
- Nao contem regra de negocio.
- Nao acessa banco diretamente.

2. Tool (dominio da ferramenta)
- Concentra regra de negocio.
- Valida entrada.
- Executa logica.
- Orquestra servicos.
- Acessa repositorios por interface.
- Nao depende de WPF.

3. Infrastructure
- Implementa acesso a dados.
- Implementa repositorios concretos.
- Acessa DbContext.
- Nao contem regra de negocio da ferramenta.

4. Banco (SQLite)
- Persistencia unica para dominio e configuracoes nomeadas.

## Regras de dependencia
Direcao permitida:
`Host -> Tool -> Infrastructure -> Database`

Dependencias proibidas:
- `Infrastructure -> Host`
- `Tool -> WPF`
- `Host -> DbContext`

## Regra de persistencia (oficial)
- Banco (`SQLite`): dados de dominio e configuracoes nomeadas.
- Arquivo (`JSON`): configuracao global da aplicacao, ambiente e artefatos tecnicos.

## Regra fundamental
A UI nunca executa logica da Tool diretamente.
Toda execucao passa pela Engine da Tool.

## Observacoes de fase
- Escopo atual da fase: Host WPF (CLI fora do escopo desta etapa).
- Antes da execucao por ferramenta, definir itens compartilhados no Core.
