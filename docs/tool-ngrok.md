# DevTools.Ngrok - Como usar (CLI)

## Objetivo

Controlar processos e tuneis ngrok.

## Passos

1. Selecione `Ngrok`.
2. Escolha a acao.
3. Informe parametros adicionais.

## Entradas

- Acao: listar, fechar, start http, kill all, status
- BaseUrl API (opcional, padrao http://127.0.0.1:4040/)
- Timeout (segundos)
- Retry count
- Nome do tunel (obrigatorio para fechar)
- Protocolo, porta, caminho do exe e args extras (para start http)

## Saida

- Lista de tuneis ou status do processo

## Observacoes

- Start http inicia o processo em background.
