# LOG DEMANDA - Snapshot Piloto (Novo Padrao)

- Demanda relacionada: `docs/definicoes/DEFINICAO_FLUXO_PADRAO_FERRAMENTAS_E_PERSISTENCIA_20260306_2233.md`
- Status: Em andamento
- Responsavel: Equipe DevTools
- Iniciado em: 2026-03-06 22:33
- Atualizado em: 2026-03-07 03:04

## Entradas de Log
- [2026-03-06 22:33] Definicao inicial criada para novo fluxo padrao de ferramentas.
- [2026-03-06 22:33] Snapshot definida como primeira ferramenta piloto.
- [2026-03-06 22:33] Checklist de pendencias da fase criado.
- [2026-03-07 00:29] Conferencia completa da estrutura de projetos apos limpeza.
- [2026-03-07 00:29] Alinhamento da solution (`src/DevTools.slnx`) para host em `src/Hosts/DevTools.Host.Wpf`.
- [2026-03-07 00:29] Alinhamento de `docs/architecture.md` com a tree real (sem CLI, host em `Hosts`).
- [2026-03-07 00:54] Fase 0 e Fase 1 marcadas como concluidas no checklist (escopo congelado + contrato base definido).
- [2026-03-07 00:54] Diretriz operacional definida: foco principal volta para execucao da Snapshot no projeto atual.
- [2026-03-07 01:04] Ordem ajustada: Snapshot fica bloqueada por demanda previa de Core compartilhado.
- [2026-03-07 02:13] Fase 2 concluida apos fechamento da demanda de Core compartilhado.
- [2026-03-07 02:13] Implementados modelos/validadores/engine da Snapshot no novo padrao.
- [2026-03-07 02:13] Implementado contrato de repositorio da Snapshot e repositorio concreto na Infrastructure.
- [2026-03-07 02:14] Build validado para `Core + Snapshot + Infrastructure` (compilacao final com sucesso).
- [2026-03-07 02:24] Nomenclatura ajustada para remover ambiguidade com EF Core: `SnapshotConfiguration*` renomeado para `SnapshotEntity*`.
- [2026-03-07 02:33] Documento operacional de fluxo da Snapshot criado para consulta de implementacao e testes.
- [2026-03-07 03:04] Importada base IDE style para o novo Host WPF (Theme + Components + validacao inline).
- [2026-03-07 03:04] MainWindow modular criada no Host e Snapshot integrada como primeiro UserControl.
- [2026-03-07 03:04] Fase 4 marcada como concluida (host orquestrando Tool/Service sem regra de negocio da ferramenta na janela principal).
- [2026-03-07 03:04] Build do Host WPF concluido com sucesso apos integracao.

## Mudancas de Escopo
- [2026-03-06 22:33] Escopo restrito para uma unica ferramenta (Snapshot) antes de replicacao.

## Evidencias
- Arquivos:
  - `docs/definicoes/DEFINICAO_FLUXO_PADRAO_FERRAMENTAS_E_PERSISTENCIA_20260306_2233.md`
  - `docs/pendencias/PENDENCIAS_SNAPSHOT_PILOTO_20260306_2233.md`
  - `docs/architecture.md`
  - `src/DevTools.slnx`
  - `src/Tools/DevTools.Snapshot/Models/SnapshotEntity.cs`
  - `src/Tools/DevTools.Snapshot/Repositories/ISnapshotEntityRepository.cs`
  - `src/Tools/DevTools.Snapshot/Services/SnapshotEntityService.cs`
  - `src/Infrastructure/DevTools.Infrastructure/Persistence/Repositories/SnapshotEntityRepository.cs`
  - `docs/definicoes/DEFINICAO_FLUXO_PROCESSO_SNAPSHOT_20260307_0233.md`
  - `src/Hosts/DevTools.Host.Wpf/App.xaml`
  - `src/Hosts/DevTools.Host.Wpf/Views/MainWindow.xaml`
  - `src/Hosts/DevTools.Host.Wpf/Views/SnapshotWorkspaceView.xaml`
  - `src/Hosts/DevTools.Host.Wpf/Services/ValidationUiService.cs`
  - `src/Hosts/DevTools.Host.Wpf/Theme/DarkTheme.xaml`
- Build/Teste:
  - `dotnet build src/Infrastructure/DevTools.Infrastructure/DevTools.Infrastructure.csproj -v minimal` (sucesso, incluindo Snapshot).
  - `dotnet build src/Hosts/DevTools.Host.Wpf/DevTools.Host.Wpf.csproj -v minimal` (sucesso).
- Observacoes:
  - O build direto de Snapshot teve lock temporario em `obj/Debug` durante a primeira tentativa.

## Encerramento
- Data/Hora:
- Resultado final:
- Proximos passos:
