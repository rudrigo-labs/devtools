# DevTools CLI – Camada de Console (V2)

Status: Em planejamento (2026-02-07)

## Objetivo

Definir a camada de console (CLI) da suite DevTools, com foco em operacao interativa por menu.

## Prioridade de UX

- O fluxo principal e interativo: menu -> perguntas -> execucao.
- Argumentos por linha de comando sao opcionais e servem como atalho.
- Se faltarem parametros via args, o CLI deve perguntar na tela.

## Requisitos funcionais

- Menu principal com todas as ferramentas registradas.
- Entrada interativa com validacao e opcao de cancelar.
- Barras de progresso quando houver percentual.
- Status textual quando nao houver percentual.
- Mensagens de sucesso/erro padronizadas.
- Console nao fecha automaticamente; o usuario escolhe continuar ou sair.
- Sempre mostrar opcoes: `1) Voltar 2) Repetir 0) Sair`.
- Todo erro (tratado ou nao) deve aparecer no console.
- Logs de erro por ferramenta em `%AppData%/DevTools/logs`.

## Padrao de comandos

- Cada tool exposta no CLI implementa um comando (IDevToolCommand).
- O comando deve aceitar args, mas priorizar prompts.
- Erros devem permitir: tentar novamente, voltar ao menu, sair.

## Progresso

- Usar IProgressReporter do Core e converter para barra no console.
- Quando Percent for null, exibir mensagem atual sem barra.

## Ferramentas com origem WPF/Tray

Estas tools podem entrar no CLI, mantendo o fluxo interativo:

- SSHTunnel
- Ngrok
- Notes

### Observacoes

- SSHTunnel deve rodar em background e reportar status/erro de conexao.
- Ngrok pode abrir outro processo/console, mas o CLI deve listar e fechar tuneis.
- Notes deve permitir ler, salvar e enviar notas com validacao de config.

## Escopo inicial sugerido

- Implementar CLI base com menu interativo.
- Adicionar comandos para Snapshot, Harvest, SearchText, Rename.
- Adicionar SSHTunnel, Ngrok e Notes com fluxo interativo.

## Documentacao por ferramenta

- Veja `docs/tools/index.md` para a lista completa e links por tool.
