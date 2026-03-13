# DevTools - Navegacao e UX de Telas

Status: ativo
Data: 2026-03-13

## Objetivo
Este documento descreve como a navegacao de telas esta organizada no Host WPF, sem alterar arquitetura (Host -> Facade -> Engine), engines, infraestrutura ou layout base.

## Fluxo principal da aplicacao
1. Start
2. Home
3. Escolher ferramenta
4. Tela de Configuracao da ferramenta
5. Tela de Execucao da ferramenta

## Home
Arquivo: `src/Host/DevTools.Host.Wpf/Views/HomeLauncherView.xaml`

A Home funciona como launcher de ferramentas com cards:
- Snapshot
- Migrations
- UTF-8 Convert
- Notes

Cada card tem:
- icone
- nome
- descricao curta
- botao `Abrir configuracao`

Ao clicar no card, a Home envia evento `OpenToolRequested` para a MainWindow, que abre a ferramenta no modo de configuracao.

## Sidebar
Arquivo: `src/Host/DevTools.Host.Wpf/Views/MainWindow.xaml`

A sidebar mantem navegacao lateral e agora inclui `Home` no topo.
- Clique em `Home`: abre a tela Home
- Clique em tool com fluxo configuracao-primeiro: abre em `Configuration`

Tools com fluxo configuracao-primeiro:
- Snapshot
- Migrations
- Utf8Convert
- Notes

## MainWindow e intents de workspace
Arquivo: `src/Host/DevTools.Host.Wpf/Views/MainWindow.xaml.cs`

A navegacao usa intents:
- `Default`
- `Configuration`
- `Execution`

Quando a tool suporta metodos de ativacao, a MainWindow aplica o modo antes de renderizar:
- `ActivateConfigurationMode()`
- `ActivateExecutionMode()`

## Separacao Configuracao x Execucao por ferramenta

### Snapshot
- Entrada via Home/Sidebar abre em configuracao.
- View alterna entre modos de configuracao e execucao.

### Migrations
- Entrada via Home/Sidebar abre em configuracao.
- Configuracao salva parametros.
- Execucao roda `dotnet ef` com os parametros.

### Notes
- Entrada via Home/Sidebar abre em configuracao.
- Modo execucao mostra lista/editor de notas.
- Modo configuracao mostra setup local + Google Drive.

### Utf8Convert
- Entrada via Home/Sidebar abre em configuracao (intent aplicado pela MainWindow).
- Mantem arquitetura atual da ferramenta.

### Demais ferramentas
- Mantem fluxo padrao atual do projeto.
- Nao houve mudanca de engine/infra.

## Regras UX aplicadas
- Home e launcher inicial (nao inicia direto em tool).
- Nome da tela ativa no topo reflete contexto (`Tool Configuration`, etc.).
- Configuracao e execucao seguem responsabilidades separadas por modo.
- Acoes de configuracao nao disparam execucao automaticamente.

## Arquivos chave alterados
- `src/Host/DevTools.Host.Wpf/Views/HomeLauncherView.xaml`
- `src/Host/DevTools.Host.Wpf/Views/HomeLauncherView.xaml.cs`
- `src/Host/DevTools.Host.Wpf/Views/MainWindow.xaml`
- `src/Host/DevTools.Host.Wpf/Views/MainWindow.xaml.cs`
- `src/Host/DevTools.Host.Wpf/App.xaml.cs`

## Observacoes
- A arquitetura de camadas foi preservada.
- Nao houve mudanca em engines das ferramentas.
- Nao houve mudanca na infraestrutura de persistencia.
