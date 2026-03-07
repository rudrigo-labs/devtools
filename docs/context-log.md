# Context Log

## 2026-03-07 01:40
- Decisao: alinhar regras locais do agente com a arquitetura oficial atual do projeto.
- Motivo: havia divergencia entre `.ai/PROJECT_RULES.md` (referencias a `src/Presentation` e `src/Shared`) e a arquitetura canonica em `docs/architecture.md` (`src/Hosts`, `src/Core`, `src/Tools`, `src/Infrastructure`).
- Impacto tecnico: reduz conflito de contexto para execucao de demandas e evita instrucoes contraditorias durante varredura e implementacao.

## 2026-03-07 01:55
- Decisao: implementar pacote base compartilhado no `Core` sem persistencia de configuracao em arquivo.
- Motivo: consolidar contratos comuns (entidade base, resultados, validacao, repositorio, UTC e slug) antes da fase por ferramenta.
- Impacto tecnico: `Core` passa a ser fonte unica de contratos reutilizaveis e bloqueia retorno de acoplamento legado (JSON store/provedores concretos).

## 2026-03-07 01:52
- Decisao: abrir demanda propria para `Infrastructure` com analise detalhada do legado antes de implementar.
- Motivo: risco alto de reproduzir problemas antigos (fallback silencioso para JSON, ausencia de baseline SQLite e acoplamentos legados).
- Impacto tecnico: definicao canonica da Infrastructure passa a guiar implementacao por fases e reduz regressao arquitetural.

## 2026-03-07 02:01
- Decisao: iniciar Infrastructure pela base tecnica (DbContext + bootstrap + baseline SQLite) antes de repositorios por ferramenta.
- Motivo: estabilizar persistencia canonica e padrao de conexao/concorrencia antes de plugar Snapshot e demais tools.
- Impacto tecnico: infraestrutura compila isolada, sem acoplamento direto com projetos de ferramenta, e pronta para migracoes versionadas.

## 2026-03-07 02:13
- Decisao: iniciar integracao por ferramenta com Snapshot como piloto (entidade completa + repositorio + servico de configuracao).
- Motivo: validar fluxo fim-a-fim no novo padrao sem espalhar mudanca em todas as ferramentas de uma vez.
- Impacto tecnico: Snapshot passa a ter contrato de configuracao persistivel no banco, com base para replicacao nas proximas tools.

## 2026-03-07 02:24
- Decisao: padronizar nomenclatura da Snapshot para `Entity` em vez de `Configuration`.
- Motivo: evitar ambiguidade com configuracao de framework/ORM e manter foco no conceito de entidade da ferramenta.
- Impacto tecnico: contratos e implementacoes renomeados para `SnapshotEntity*`, sem alterar comportamento funcional.

## 2026-03-07 02:29
- Decisao: concluir primeiro a Snapshot fim-a-fim antes de expandir para outras ferramentas.
- Motivo: reduzir paralelismo e garantir fechamento completo de uma ferramenta por vez.
- Impacto tecnico: retirada da exigencia de cobrir +2 ferramentas na fase atual da Infrastructure.

## 2026-03-07 02:33
- Decisao: criar documento dedicado de fluxo da Snapshot para consulta operacional.
- Motivo: evitar perda de contexto durante implementacao e testes de fechamento fim-a-fim.
- Impacto tecnico: referencia unica do processo da ferramenta do host ate persistencia/execucao.

## 2026-03-07 02:38
- Decisao: consolidar tutorial operacional unico para criacao de novas ferramentas.
- Motivo: padronizar onboarding tecnico e reduzir variacao de implementacao entre demandas.
- Impacto tecnico: fluxo de criacao passa a ter checklist oficial, com rastreabilidade obrigatoria no pacote minimo da demanda.
