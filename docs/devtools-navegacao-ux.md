# DevTools - Navegação, UX e Padronização

Status: ativo  
Data: 2026-03-15

## Objetivo
Descrever o comportamento real de navegação e o padrão atual das telas de ferramentas no Host WPF.

## Navegação principal (estado atual)
1. A aplicação inicia em `Ferramentas` (launcher de execução).
2. O launcher de execução (`HomeLauncherView`) abre as ferramentas em modo de execução.
3. O launcher de configuração (`ConfigurationLauncherView`) abre as ferramentas em modo de configuração.
4. A sidebar mantém acesso direto por:
- `Exec:<Tool>` (execução)
- `Cfg:<Tool>` (configuração, quando a ferramenta suporta esse modo)

## Launchers

### Ferramentas (`HomeLauncherView`)
Cards disponíveis:
- Snapshot
- Rename
- Harvest
- ImageSplit
- SearchText
- Organizer
- Utf8Convert
- Migrations
- SshTunnel
- Ngrok
- Notes

### Configurações (`ConfigurationLauncherView`)
Cards disponíveis:
- Snapshot
- Harvest
- Organizer
- Migrations
- SshTunnel
- Ngrok
- Notes

## Intents e ativação de modo
`MainWindow` usa intents:
- `Default`
- `Configuration`
- `Execution`

`ApplyWorkspaceIntent` aplica `ActivateConfigurationMode()` e `ActivateExecutionMode()` para:
- Snapshot
- Harvest
- Organizer
- Migrations
- SshTunnel
- Ngrok
- Notes

Ferramentas de execução direta (sem modo de configuração no fluxo atual):
- Rename
- ImageSplit
- SearchText
- Utf8Convert

## Padronização de configuração (exceto Notes)
Ferramentas: Snapshot, Harvest, Organizer, Migrations, SshTunnel e Ngrok.

### ActionBar no modo configuração
- Exibe: `Novo`, `Salvar`, `Cancelar` (e `Ajuda`).
- Exceção: no `Organizer`, o botão `Excluir` também fica visível em configuração.
- Oculta: `Histórico`, `Ir para ferramenta`, `Voltar`.
- Regra de habilitação:
1. `Novo` habilitado somente quando não existe rascunho.
2. Após clicar em `Novo`, `Novo` é desabilitado.
3. `Salvar` e `Cancelar` ficam habilitados para o rascunho atual.
4. Ao cancelar, volta ao estado inicial da configuração.

### Comportamento de `Novo`
Ao clicar em `Novo` no modo configuração:
1. Cria um rascunho local não persistido.
2. Nome padrão do rascunho:
- `Snapshot 1`
- `Harvest 1`
- `Organizer 1`
- `Migrations 1`
- `SSH Tunnel 1`
- `Ngrok 1`

### Comportamento de `Cancelar`
Ao clicar em `Cancelar` no modo configuração:
1. Descarta o rascunho.
2. Limpa o objeto corrente de configuração.
3. Remove o nome temporário da tela (volta para entidade não vinculada).
4. Reabilita `Novo`.
5. Desabilita novamente `Salvar` e `Cancelar`.

## Persistência de configurações das ferramentas
As configurações de ferramentas persistem no banco SQLite em `tool_configurations`:
- identificação por `tool_slug` + `name`
- carga útil em `payload_json`

Observação:
- `appsettings.json` permanece para configurações gerais do host.
- Perfis das ferramentas não são persistidos em arquivo JSON de tela.

## Organizer (categorias)
No modo de configuração do Organizer:
1. A lista de categorias ocupa a largura total do bloco de categorias.
2. O editor de categoria fica abaixo da lista.
3. É possível adicionar/remover categorias dinamicamente.
4. A configuração salva leva as categorias no payload da ferramenta.

## Exceção conhecida
`Notes` mantém fluxo próprio (configuração + execução no mesmo workspace com comportamento específico de editor), portanto não segue integralmente o mesmo padrão das demais telas configuráveis.
