# Plano de Migracao de UI IDE-Style (WPF)

## Objetivo
Aplicar o guideline IDE-style em todo o DevTools WPF com entregas incrementais e risco controlado.

## Escala de esforco
- `Baixo`: ajuste isolado de estilo/token, sem impacto estrutural.
- `Medio`: ajuste de template com impacto visual mais amplo.
- `Alto`: refatoracao de layout por janela com validacao detalhada.

## Fases
1. Fase 1 - Base do tema e controles globais.
2. Fase 2 - Estrutura compartilhada de frame (header/content/footer) e consistencia de acoes.
3. Fase 3 - Migracao do conteudo das janelas de ferramentas.
4. Fase 4 - Migracao das janelas especiais (`MainWindow` configuracoes/perfis, `Notes`, `Help`) e endurecimento visual.

## Esforco por arquivo

### Fase 1 (concluida)
- `Theme/Foundation/Spacing.xaml` - `Baixo`
- `Theme/Controls/Control.xaml` - `Baixo`
- `Theme/Controls/TextBox.xaml` - `Medio`
- `Theme/Controls/ComboBox.xaml` - `Medio`
- `Theme/Controls/Button.xaml` - `Medio`
- `Theme/Controls/Scroll.xaml` - `Baixo`
- `Theme/Components/ToolFrame.xaml` - `Medio`

### Fase 2 (concluida)
- `Theme/Foundation/Typography.xaml` - `Baixo`
- `Theme/Foundation/Colors.xaml` - `Baixo`
- `Theme/Controls/Window.xaml` - `Baixo`
- `Theme/Controls/CheckBox.xaml` - `Baixo`
- `Theme/Controls/Menu.xaml` - `Baixo`

### Fase 3 (concluida)
- `Views/HarvestWindow.xaml` - `Medio`
- `Views/OrganizerWindow.xaml` - `Medio`
- `Views/RenameWindow.xaml` - `Medio`
- `Views/SnapshotWindow.xaml` - `Medio`
- `Views/ImageSplitWindow.xaml` - `Medio`
- `Views/Utf8ConvertWindow.xaml` - `Medio`
- `Views/SearchTextWindow.xaml` - `Medio`
- `Views/MigrationsWindow.xaml` - `Medio`
- `Views/NgrokWindow.xaml` - `Alto`
- `Views/SshTunnelWindow.xaml` - `Alto`
- `Views/JobCenterWindow.xaml` - `Medio`
- `Views/LogsWindow.xaml` - `Baixo`

### Fase 4
- `Views/MainWindow.xaml` (configuracoes/perfis) - `Alto`
- `Views/NotesWindow.xaml` - `Alto`
- `Views/HelpWindow.xaml` - `Medio`
- Ajustes relacionados em `Views/*.xaml.cs` - `Medio`

## Criterios de aceite
- Altura visual e padding uniformes nos inputs.
- Header fixo em 48px e footer entre 64-80px no frame compartilhado.
- Sem sombras pesadas nos containers principais e nas acoes primarias.
- No maximo uma area de scroll por regiao funcional.
- Validacao de build sem erros.

## Status atual
- Fase 1: concluida.
- Fase 2: concluida.
- Fase 3: concluida.
- Fase 4: concluida.
- Entregas concluidas na Fase 4:
- `Views/MainWindow.xaml`: configuracoes/perfis migrados para `DevToolsInputLabel`, `DevToolsPrimaryButton`, `DevToolsSecondaryButton`.
- `Views/NotesWindow.xaml`: inputs migrados para `DevToolsTextInput` e container alinhado aos tokens de tema.
- `Views/HelpWindow.xaml`: header/footer e botoes de acao alinhados aos tokens IDE-style.
- `Views/MainWindow.xaml`: endurecimento estrutural aplicado (header 48px, footer fixo, superficies tokenizadas, espacamento de conteudo).
- `Views/MainWindow.xaml`: refinamento opcional concluido; cores hardcoded remanescentes (`White`/hex) substituidas por tokens de tema.
- `Views/MainWindow.xaml.cs`/`App.xaml.cs`: fluxo de teste do Google Drive ajustado para usar servico injetado, chamada assincrona sem bloqueio, trava de reentrada e aviso unificado para campos obrigatorios.
