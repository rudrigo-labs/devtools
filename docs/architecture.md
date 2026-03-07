# Arquitetura - Nova Fase

Fonte canonica atual:
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
├─ src
│  ├─ Hosts
│  │  └─ DevTools.Presentation.Wpf
│  │
│  ├─ Core
│  │  ├─ Abstractions
│  │  ├─ Contracts
│  │  └─ Results
│  │
│  ├─ Tools
│  │  ├─ Snapshot
│  │  │  ├─ Models
│  │  │  ├─ Validation
│  │  │  ├─ Engine
│  │  │  └─ Repositories (interfaces)
│  │  ├─ Notes
│  │  │  ├─ Models
│  │  │  ├─ Validation
│  │  │  ├─ Engine
│  │  │  └─ Repositories (interfaces)
│  │  └─ ... (demais tools no mesmo padrao)
│  │
│  └─ Infrastructure
│     ├─ Persistence
│     │  ├─ DbContext
│     │  ├─ Repositories (implementacoes)
│     │  └─ Migrations
│     └─ Integrations
│
└─ docs
   ├─ definicoes
   ├─ controle
   └─ pendencias
```

Fluxo canonico:
`WPF Host -> Tool.Engine -> Tool.Repositories -> Infrastructure -> Banco`

Historico anterior:
- `legacy-v1/docs/architecture.md`
