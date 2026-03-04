# DevTools.SSHTunnel - Como usar (CLI)

## Objetivo

Criar e controlar tunel SSH em background.

## Passos

1. Selecione `SSH Tunnel`.
2. Escolha a acao (start, stop ou status).
3. Para start, informe os dados do tunel.

## Entradas

- Host, porta e usuario SSH
- Bind local e porta local
- Host remoto e porta remota
- Identity file (opcional)
- StrictHostKeyChecking (default/yes/no/accept-new)
- Timeout de conexao (opcional)

## Saida

- Estado do tunel
- PID do processo (quando disponivel)
- Ultimo erro (se houver)
