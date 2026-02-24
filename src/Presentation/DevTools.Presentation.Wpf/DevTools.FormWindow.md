# Padrão DevTools FormWindow (WPF)

Documento de referência para a **reimplementação da UI** das ferramentas WPF do DevTools, com foco em:

- Separar **casca (Window)** de **conteúdo (UserControl)**.
- Garantir um **padrão visual único** entre ferramentas (SSH, Ngrok, Migrations, Harvest, Organizer, etc.).
- Permitir reaproveitar o mesmo formulário em:
  - janelas próprias (tools),
  - abas de configuração (SettingsTabView),
  - e futuros hubs/dashboards.

---

## 1. Objetivo

Reimplementar as telas de ferramenta do DevTools (WPF), mexendo **apenas em layout/XAML**, mantendo:

- toda a lógica de negócio,
- serviços,
- comandos,
- code-behind existente.

Escopo da reimplementação:

- **Descartável:** XAML de layout (grids, margens, labels, cores hardcoded, footers duplicados).
- **Intocável:** C# (ViewModels, services, handlers), componentes reutilizáveis (PathSelector, ProfileSelector, etc.).

---

## 2. Arquitetura de tela

### 2.1. Window (casca / shell)

Responsabilidades:

- Aplicar `ModernWindowStyle` (borda, sombra, cantos).
- Header padrão:
  - título da ferramenta,
  - botão de ajuda (`?`),
  - botão fechar (`✕`),
  - suporte a drag (MouseLeftButtonDown).
- Rodapé cinza padrão com:
  - status à esquerda,
  - botões de ação à direita com mesma largura.
- Hospedar o **UserControl da ferramenta** no miolo da tela.

Não é responsabilidade da Window:

- layout interno de campos (labels, TextBox, ComboBox, etc.);
- detalhes de seções e grids da ferramenta.

### 2.2. UserControl (conteúdo da ferramenta)

Responsabilidades:

- Layout do formulário da ferramenta:
  - ProfileSelector, PathSelector,
  - campos (TextBox/ComboBox/CheckBox),
  - seções lógicas (Conexão, Opções, Logs, etc.).
- Organização em grids/stackpanels:
  - campos simples,
  - pares (Host/Porta),
  - grupos de seções.
- Bindings para propriedades/comandos já existentes (DataContext atual).

Não é responsabilidade do UserControl:

- header da janela (título, ícones, fechar);
- rodapé (botões OK/Cancelar, status);
- decidir se está em uma Window, Tab ou Dashboard.

---

## 3. Tema e estilos globais

Tudo que for cor/fonte/altura de controle deve vir de `DarkTheme.xaml`:

- **Brushes principais**
  - `WindowBackgroundBrush`
  - `ControlBackgroundBrush`
  - `PrimaryTextBrush`
  - `SecondaryTextBrush`
  - `AccentBrush`
  - `BorderBrush`

- **Inputs**
  - `ModernTextBoxStyle`
    - Fonte 14
    - MinHeight ≈ 40
    - Padding “gordinho”
    - Fundo mais escuro (WindowBackgroundBrush)
  - `ModernComboBoxStyle`
    - Fonte 14
    - MinHeight ≈ 40
    - fundo um pouco mais claro (ControlBackgroundBrush)
  - Defaults:
    - `Style TargetType="TextBox" BasedOn="{StaticResource ModernTextBoxStyle}"`
    - `Style TargetType="ComboBox" BasedOn="{StaticResource ModernComboBoxStyle}"`

- **Botões**
  - `PrimaryButtonStyle`
  - `SecondaryButtonStyle`
  - `DangerButtonStyle`
  - `IconButtonStyle`

- **Rodapé**
  - `FooterBarStyle` (Border cinza padrão do rodapé)

Regras:

- Evitar `MinHeight`, `FontSize`, `Background`, `Foreground` hardcoded em controles nas Views.
- Usar sempre brush/Style do tema em vez de códigos de cor soltos (`#CCCCCC`, `#333333`, etc.).

---

## 4. Padrão de layout do UserControl (formulário)

O UserControl é o “formulário” da ferramenta. Padrão visual:

### 4.1. Estrutura base

- Raiz: normalmente `Grid` ou `StackPanel` vertical.
- Dentro, seções bem delimitadas (ex.: Conexão SSH, Mapeamento de túnel, Logs).

### 4.2. Labels

- Sempre `TextBlock`.
- Nunca usar `Label`.
- Padrão:
  - `FontSize="14"`
  - `Foreground="{StaticResource SecondaryTextBrush}"`
  - `Margin="0,0,0,6"` (respiro embaixo do texto).

### 4.3. Inputs

- `TextBox`:
  - `Style="{StaticResource ModernTextBoxStyle}"`
  - Não definir `MinHeight`, `FontSize` ou `Background` direto no controle.

- `ComboBox`:
  - `Style="{StaticResource ModernComboBoxStyle}"`
  - Também sem overrides de tamanho/cores locais.

### 4.4. Layout de campos

- Campo simples:
  - `StackPanel` vertical:
    - TextBlock (label) em cima,
    - TextBox/ComboBox embaixo.

- Campos em par (Host/Porta, etc.):
  - `Grid` com colunas proporcionais:
    - ex.: `3*` e `1*` ou similar.
  - Cada coluna possui seu próprio `StackPanel` (label + input).

- Seções:
  - Título de seção em `TextBlock`:
    - `FontSize="14"`
    - `FontWeight="SemiBold"`
    - `Foreground="{StaticResource AccentBrush}"` ou `PrimaryTextBrush`
    - `Margin` consistente acima/abaixo.

- Largura:
  - Quando necessário, limitar `MaxWidth` do bloco principal (ex.: 600–720) para evitar forms esticados demais em telas grandes.

### 4.5. Scroll

- Regra: **no máximo 1 ScrollViewer vertical por tela**.
- O UserControl, em geral, **não define ScrollViewer**.
  - Quem decide scroll é a Window/container que hospeda o UserControl.
- Horizontal:
  - `HorizontalScrollBarVisibility="Disabled"` na maioria dos casos.
  - Exceto telas de log/tabela onde horizontal faça sentido.

---

## 5. Padrão da Window (DevTools FormWindow)

A Window padrão de ferramenta segue um layout em 3 linhas: header, conteúdo, footer.

### 5.1. Grid principal

```xaml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="40"/>   <!-- Header -->
        <RowDefinition Height="*"/>    <!-- Conteúdo (UserControl) -->
        <RowDefinition Height="80"/>   <!-- Footer -->
    </Grid.RowDefinitions>
    ...
</Grid>
```

### 5.2. Header (Row 0)

- `Border` com fundo dark (ex.: `#2D2D30` ou brush equivalente).
- `Grid` interno com 3 colunas:
  - Coluna 0: título da ferramenta (TextBlock, FontSize 16, SemiBold, `PrimaryTextBrush`).
  - Coluna 1: botão de ajuda (`?`, `IconButtonStyle`).
  - Coluna 2: botão fechar (`✕`, `IconButtonStyle`).
- Handler de drag:
  - `MouseLeftButtonDown="Header_MouseLeftButtonDown"`.

### 5.3. Conteúdo (Row 1)

- Um `ScrollViewer` vertical envolvendo o UserControl da ferramenta:

```xaml
<ScrollViewer Grid.Row="1"
              Margin="30,20,30,20"
              VerticalScrollBarVisibility="Auto"
              HorizontalScrollBarVisibility="Disabled">
    <views:NomeDaFerramentaView />
</ScrollViewer>
```

- `NomeDaFerramentaView` é o UserControl com o formulário (SSH, Ngrok, etc.).

### 5.4. Rodapé (Row 2)

- `Border` com `Style="{StaticResource FooterBarStyle}"`:
  - `Background=ControlBackgroundBrush`
  - `BorderBrush=BorderBrush`
  - `BorderThickness="0,1,0,0"`
  - `CornerRadius="0,0,12,12"`
  - `Padding="30,10"`

- Dentro do Border:
  - `Grid` com `Grid.IsSharedSizeScope="True"`.
  - Coluna 0: área de status (icone + texto).
  - Colunas à direita: botões de ação com mesma largura, usando `SharedSizeGroup="ActionButtons"`:
    - botão secundário (Cancelar),
    - botão primário (Executar/Conectar/etc.),
    - eventualmente Danger (ex.: “Matar processos Ngrok”).

---

## 6. Fases da reimplementação

### Fase 1 — Definição do padrão (este documento)

- Definir:
  - Arquitetura Window x UserControl.
  - Padrão visual do formulário (labels, inputs, seções).
  - Padrão da Window (header, conteúdo com ScrollViewer, rodapé padrão).
  - Uso dos estilos e brushes globais.
- Saída:
  - Este documento (`DevTools.FormWindow.md`).

### Fase 2 — Ferramenta piloto

- Escolher 1 ferramenta para pilotar (ex.: SSH / SshTunnel).
- Para a ferramenta piloto:
  - Criar UserControl de conteúdo (ex.: `SshTunnelView`):
    - Extrair do XAML atual tudo que é “miolo de formulário”.
    - Ajustar layout para seguir o padrão deste documento.
  - Ajustar a Window da ferramenta:
    - Transformar em FormWindow padrão (header, conteúdo, rodapé).
    - Hospedar o UserControl em `Grid.Row=1` dentro de um ScrollViewer.
  - Se houver painel da mesma ferramenta em SettingsTabView:
    - substituir UI duplicada por `<views:SshTunnelView />`.
- Validação:
  - Abrir a Window e o painel Settings da ferramenta.
  - Conferir:
    - consistência visual,
    - alinhamento de inputs,
    - comportamento de scroll,
    - rodapé padrão.

### Fase 3 — Outras ferramentas principais

- Repetir o padrão (UserControl + FormWindow + Tab) para:
  - Ngrok,
  - Migrations,
  - Harvest,
  - Organizer,
  - e demais tools que façam sentido.
- Sem fazer tudo de uma vez:
  - reimplementar uma ferramenta,
  - validar visualmente,
  - só então ir para a próxima.

### Fase 4 — SettingsTabView / Dashboard / Hubs

- Substituir UI duplicada por UserControls já existentes.
- Garantir que:
  - qualquer lugar que exiba o “formulário da ferramenta” use o mesmo UserControl,
  - o visual seja idêntico entre:
    - Window da ferramenta,
    - tab de Settings,
    - widgets em dashboards/hubs (quando existirem).

---

## 7. Regras gerais

- Não alterar lógica de negócio na reimplementação de UI.
- Não introduzir cores hardcoded ou estilos locais quando houver equivalentes no tema.
- Sempre que possível:
  - usar UserControls para blocos reutilizáveis (PathSelector, ProfileSelector, formulários inteiros de tool),
  - manter o padrão FormWindow para ferramentas com janela própria.

