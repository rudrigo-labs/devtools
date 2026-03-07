# DEFINICAO - Fluxo Padrao de Ferramentas e Persistencia

- Status: Aberto
- Responsavel: Equipe DevTools
- Criado em: 2026-03-06 22:33
- Atualizado em: 2026-03-06 22:33
- Tema: Fluxo unico de arquitetura para todas as ferramentas
- Origem da demanda: Redefinicao para reduzir risco e retrabalho

## Objetivo
Definir um padrao unico de construcao de ferramenta para garantir:
- independencia de interface (WPF, CLI ou qualquer host),
- previsibilidade tecnica,
- replicacao segura do mesmo modelo para todas as tools.

## Escopo
- Inclui:
  - fluxo oficial de chamada entre camadas,
  - regra de persistencia (banco + arquivo),
  - contrato base de configuracao nomeada,
  - adocao inicial via ferramenta piloto `Snapshot`.
- Nao inclui:
  - migracao completa de todas as ferramentas nesta etapa,
  - alteracoes visuais de UI fora do necessario para o piloto.

## Regras e Definicoes
1. Direcao de chamada (obrigatoria):
   - `Host (WPF/CLI)` -> `Tool.Engine` -> `Tool.Repositories (interfaces)` -> `Infrastructure`.
   - retorno sempre no caminho inverso.
2. Proibicoes:
   - Tool nao referencia WPF.
   - Tool nao referencia `DbContext`/EF/SQLite direto.
   - Host nao executa regra de negocio da ferramenta.
3. Persistencia desta fase:
   - Banco (`SQLite`): dados estruturados de dominio e configuracoes nomeadas de ferramenta.
   - Arquivo (`JSON`): configuracoes globais do host/aplicacao e valores de ambiente.
4. Modelo base de configuracao nomeada (canonico):
   - `Id` (slug canonico)
   - `Name`
   - `Description`
   - `IsActive`
   - `CreatedAtUtc`
   - `UpdatedAtUtc`
5. Padrao minimo por ferramenta:
   - `Models` (Request/Response/tipos de apoio)
   - `Validation`
   - `Engine`
   - `Repositories` (interfaces de dominio)
   - `Providers` (opcional)

## Ferramenta piloto
- Ferramenta inicial: `Snapshot`.
- Criterio: esta estavel funcionalmente e adequada para validar o novo padrao.

## Criterios de Conclusao
- [ ] Fluxo e contratos aprovados sem ambiguidade.
- [ ] `Snapshot` implementada integralmente no novo padrao.
- [ ] Build e testes passando apos piloto.
- [ ] Checklist da fase fechado para liberar replicacao nas demais tools.

## Riscos e Dependencias
- Risco: manter comportamento legado e novo simultaneamente por muito tempo.
- Risco: desvio de padrao por urgencia de feature.
- Dependencia: congelar mudancas fora do escopo do piloto.

## Historico de Decisoes
- 2026-03-06 22:33 - Decidido reiniciar a fase de arquitetura por piloto unico (`Snapshot`) antes de expandir.
