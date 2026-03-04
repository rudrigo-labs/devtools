# IDE Hybrid Execution Plan (WPF)

Status: Em execucao (inicio em 2026-03-04)

## Objetivo

Migrar o DevTools para um modelo hibrido, mantendo o que ja funciona hoje:

- Shell principal como ponto central de navegacao.
- Ferramentas com modo de abertura configuravel por ferramenta.
- Suporte a background para recursos de longa duracao (ex.: SSH tunnel) sem dependencia da UI visivel.

## Modelo alvo

Cada ferramenta passa a ter um `LaunchMode`:

- `DetachedWindow`: abre em janela separada (comportamento atual).
- `EmbeddedTab`: abre no Shell (aba/painel interno).
- `BackgroundOnly`: nao abre tela, apenas aciona servico/status.

## Fases

### Fase 1 - Fundacao de roteamento (sem regressao visual)

Escopo:

- Remover duplicacao de roteamento no `TrayService`.
- Criar registro central de ferramentas com `LaunchMode`.
- Preservar comportamento atual em `DetachedWindow`.

Resultado esperado:

- Base pronta para mover ferramenta por ferramenta para Shell sem reescrever todo o app.

### Fase 2 - Host de conteudo no Shell

Escopo:

- Criar host de ferramentas no `MainWindow` (aba interna ou frame).
- Integrar `EmbeddedTab` para primeiras ferramentas candidatas.

Resultado esperado:

- Primeiras ferramentas rodando no Shell mantendo tray funcional.

### Fase 3 - Servicos de background e ciclo de vida

Escopo:

- Formalizar servicos de longa duracao (ex.: SSH) independentes da janela.
- Fechamento da app com regras de tray e confirmacao de saida.

Resultado esperado:

- UI pode esconder/restaurar sem derrubar servicos ativos.

### Fase 4 - Consolidacao de UX IDE-style

Escopo:

- Unificar header/content/footer e navegacao lateral.
- Convergir telas para padrao visual unico.

Resultado esperado:

- Experiencia consistente em todas as telas.

## Esforco por arquivo (inicio)

### Fase 1

- `src/Presentation/DevTools.Presentation.Wpf/Services/TrayService.cs`: Alto
- `src/Presentation/DevTools.Presentation.Wpf/Views/MainWindow.xaml.cs`: Baixo
- `src/Presentation/DevTools.Presentation.Wpf/Theme/TrayResources.xaml`: Baixo
- `docs/IDE_HYBRID_EXECUTION_PLAN.md`: Baixo

### Fase 2 (previsao)

- `src/Presentation/DevTools.Presentation.Wpf/Views/MainWindow.xaml`: Alto
- `src/Presentation/DevTools.Presentation.Wpf/Views/MainWindow.xaml.cs`: Medio
- `src/Presentation/DevTools.Presentation.Wpf/Components/DevToolsToolFrame.xaml(.cs)`: Medio
- `src/Presentation/DevTools.Presentation.Wpf/Views/ToolsTabView.xaml(.cs)`: Medio

### Fase 3 (previsao)

- `src/Presentation/DevTools.Presentation.Wpf/Services/*` (tray/window/tunnel lifecycle): Alto
- `src/Presentation/DevTools.Presentation.Wpf/App.xaml.cs`: Medio

### Fase 4 (previsao)

- `src/Presentation/DevTools.Presentation.Wpf/Theme/*`: Alto
- `src/Presentation/DevTools.Presentation.Wpf/Views/*`: Alto

## Riscos e mitigacao

- Risco: regressao no abrir/fechar de ferramentas.
  - Mitigacao: manter `DetachedWindow` como default na Fase 1.
- Risco: regras de owner/minimize em conflito com tray.
  - Mitigacao: concentrar comportamento no roteador unico.
- Risco: migracao grande de uma vez.
  - Mitigacao: rollout incremental ferramenta por ferramenta.

## Checklist da Fase 1

- [x] Definir plano tecnico fechado.
- [x] Criar registro unico de ferramentas no `TrayService`.
- [x] Usar registro tambem no menu da bandeja (eliminar switch duplicado).
- [x] Validar build da WPF apos refactor.

## Checklist da Fase 2

- [x] Criar host embutido no `MainWindow` (aba dedicada).
- [x] Expor evento de requisicao de `EmbeddedTab` no `TrayService`.
- [x] Migrar primeiro candidato para `EmbeddedTab` (`Logs`).
- [x] Migrar segundo candidato para `EmbeddedTab` (`Jobs`).

## Checklist da Fase 3

- [x] Introduzir encerramento seguro via Tray (`RequestExitAsync`).
- [x] Bloquear saida silenciosa quando houver jobs/tunel ativos (confirmacao).
- [x] Encerrar tunel SSH no desligamento final da aplicacao.
- [x] Compartilhar `TunnelService` no ciclo de vida da app (independente da janela SSH).
- [x] Exibir dialogo de 3 opcoes ao fechar no `X` (minimizar / encerrar e sair / cancelar).

## Checklist da Fase 4

- [x] Definir estilos reutilizaveis de titulos IDE-style (`DevToolsPageTitle`, `DevToolsSectionTitle`, `DevToolsPanelTitle`, `DevToolsCardTitle`).
- [x] Padronizar navegacao de retorno nos paineis de configuracao da `MainWindow` (`SettingsBackButtonStyle`).
- [x] Padronizar cards e cabecalhos na area de configuracoes com estilo unico (`SettingsCardStyle` + `DevToolsCardTitle`).
- [x] Remover alturas fixas conflitantes em botoes/campos de configuracao (usar `MinHeight` quando multiline).
- [x] Migrar `HelpWindow` para `DevToolsToolFrame` (header/content/footer unificado).
