# DEFINICAO - Arquitetura, Persistencia e Orquestracao

- Status: Em andamento
- Criado em: 2026-03-07 00:31
- Tema: Base arquitetural da nova fase

## Objetivo
Consolidar a arquitetura canonica para todas as ferramentas:
- separacao clara entre `Host`, `Core`, `Tools` e `Infrastructure`;
- fluxo de chamada unico;
- regra unica de persistencia (banco x arquivo).

## Escopo
1. Definir camadas e responsabilidades.
2. Definir direcao obrigatoria de chamada.
3. Definir regra de persistencia.
4. Registrar regras para evitar desvio em novas demandas.

## Fluxo canonico
`WPF Host -> Tool.Engine -> Tool.Repositories -> Infrastructure -> Banco`

## Persistencia canonica
1. Banco (`SQLite`): dados de dominio e configuracoes nomeadas.
2. Arquivo (`JSON`): configuracao global, ambiente e artefatos tecnicos.

## Referencias
- `docs/definicoes/DEFINICAO_FLUXO_PADRAO_FERRAMENTAS_E_PERSISTENCIA_20260306_2233.md`
- `docs/definicoes/DEFINICAO_CAMADAS_ARQUITETURA_20260306_2345.md`
- `docs/controle/regras/REGRA_FLUXO_E_PERSISTENCIA_20260306_2339.md`
