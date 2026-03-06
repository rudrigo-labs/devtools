# Definicao - Configuracoes e Orquestracao

Data: 2026-03-06 11:17
Status: Em definicao ativa
Escopo: DevTools (WPF + Tools)

## Contexto

Este documento consolida a definicao arquitetural para encerrar o descontrole de configuracoes/configuracoes.
O foco e simplificar o modelo, remover redundancia e manter separacao correta de camadas.

## Definicoes Fechadas

### 1) Camada de apresentacao (WPF)

WPF e camada de orquestracao/UI.
WPF:
- coleta dados de tela
- chama servicos/engines
- exibe retorno, status e erros

WPF nao:
- contem regra de negocio da ferramenta
- persiste configuracao de negocio

### 2) Dominio das ferramentas

A regra de negocio deve ficar em `src/Tools/DevTools.*`.
Cada ferramenta executa com seu contrato (`*Request`) e validadores proprios.

### 3) Fim do conceito de "configuracao" separado

Nao existe mais "configuracao" como camada paralela.
Passa a existir uma entidade unica de configuracao por ferramenta.

### 4) Entidade base de configuracao (conceitual)

Campos base (comuns):
- `ToolSlug` (id logico da ferramenta; nao e id de tabela)
- `Name`
- `Description`
- `IsActive`
- `IsDefault` (opcional por ferramenta)
- `CreatedAt`
- `UpdatedAt`

Observacao:
- `ToolSlug` pode repetir entre varios registros (1..N configuracoes por ferramenta).

### 5) Modelo de uso

Para cada ferramenta:
- usuario pode executar sem configuracao salva
- usuario pode salvar varias configuracoes
- usuario pode marcar uma padrao
- usuario pode desativar sem excluir

### 6) Persistencia

Direcao definida:
- persistencia operacional sai da WPF
- fonte principal de persistencia: SQLite
- arquivo de configuracao, quando existir, deve ficar apenas para bootstrap minimo/infra

### 7) Defaults

Defaults devem facilitar uso, sem bloquear execucao.
Defaults devem ser configuraveis quando fizer sentido.

### 8) Ferramentas de texto (regra funcional)

Ferramentas focadas em texto/codigo (ex.: Harvest):
- devem operar em conteudo textual permitido
- devem bloquear binarios (ex.: `.dll`, `.exe`, `.pdb`, etc.)
- devem permitir configuracao do que e relevante (criterios de busca/selecao)

Observacao:
- Harvest e ferramenta de extracao de codigo relevante, nao "organizador de arquivo generico".

### 9) Argumentos adicionais

Decisao de direcao:
- argumentos adicionais nao devem ficar disponiveis para uso normal agora
- codigo legado pode permanecer guardado, mas sem exposicao/consumo operacional

Referencia de diagnostico:
- `docs/pendencias/VARREDURA_ARGUMENTOS_FERRAMENTAS_20260306_0943.md`

## Definicoes em Aberto (para fechar com regras)

1. Lista final de campos obrigatorios por ferramenta.
2. Regra final de `IsDefault` (quantidade permitida por ferramenta).
3. Estrategia de migracao dos dados atuais (JSON/configuracoes legados -> novo modelo).
4. Politica de validacao inline x validacao de dominio (fronteira exata).
5. Convencoes finais de nomes tecnicos (classe base/interface/DTO).

## Nota de Governanca

Este documento e base de decisao.
A implementacao deve seguir esta definicao e as regras formais que serao adicionadas em seguida.

