# Arquitetura - Nova Fase

Fonte canonica atual:
- `docs/definicoes/DEFINICAO_FLUXO_ARQUITETURAL_OFICIAL_20260307_0112.md`
- `docs/definicoes/DEFINICAO_ESTRUTURA_PADRAO_TOOL_20260307_0117.md`
- `docs/definicoes/DEFINICAO_PADRAO_REQUEST_RESULT_20260307_0117.md`
- `docs/definicoes/DEFINICAO_REGRAS_IMPLEMENTACAO_NOVAS_TOOLS_20260307_0123.md`
- `docs/definicoes/DEFINICAO_CICLO_VIDA_TOOL_20260307_0125.md`
- `docs/definicoes/DEFINICAO_FLUXO_PADRAO_FERRAMENTAS_E_PERSISTENCIA_20260306_2233.md`
- `docs/definicoes/DEFINICAO_CAMADAS_ARQUITETURA_20260306_2345.md`
- `docs/controle/regras/REGRA_FLUXO_E_PERSISTENCIA_20260306_2339.md`

Resumo:
- Host apenas chama Tool.
- Tool concentra regra de negocio e contratos.
- Infrastructure implementa detalhes tecnicos de persistencia.
- Piloto atual: Snapshot.

Tree canonica (sem CLI):
```text
DevTools
+- src
   +- Hosts
   |  +- DevTools.Host.Wpf
   |
   +- Core
   |  +- Abstractions
   |  +- Contracts
   |  +- Results
   |
   +- Tools
   |  +- Snapshot
   |  |  +- Models
   |  |  +- Validation
   |  |  +- Engine
   |  |  +- Repositories (interfaces)
   |  +- Notes
   |  |  +- Models
   |  |  +- Validation
   |  |  +- Engine
   |  |  +- Repositories (interfaces)
   |  +- ... (demais tools no mesmo padrao)
   |
   +- Infrastructure
      +- Persistence
      |  +- DbContext
      |  +- Repositories (implementacoes)
      |  +- Migrations
      +- Integrations

+- docs
   +- definicoes
   +- controle
   +- pendencias
```

Fluxo canonico:
`WPF Host -> Tool.Engine -> Tool.Repositories -> Infrastructure -> Banco`

Historico anterior:
- `legacy-v1/docs/architecture.md`
