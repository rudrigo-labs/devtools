# DevTools - Pendencias e Melhorias (Proxima Versao)

Data: 2026-03-05
Status: Aberto

Este arquivo deve conter somente itens que ainda nao foram entregues.

## 1) Estabilizacao final de testes WPF

- Problema: 2 testes seguem com `Skip` por afinidade de thread WPF no xUnit.
- Itens:
  - criar fixture STA unica para testes WPF
  - remover `Skip` de `PathSelectorTests` e `SnapshotWindowTests`
- Aceite:
  - suite completa sem `skip` nesses cenarios

## 2) Validacao E2E real do Ngrok

- Problema: ambiente de dev sem `ngrok.exe` impede teste real do tunel.
- Itens:
  - instalar ngrok localmente
  - validar onboarding + salvar token
  - validar `StartTunnel`/`StopTunnel` com URL publica na API local
- Aceite:
  - evidencia de tunel real ativo e parada controlada

## 3) Janela Sobre (acabamento)

- Problema: janela ainda precisa revisao final de conteudo/visual.
- Itens:
  - revisar texto institucional
  - revisar layout final e hierarquia visual
  - validar link para GitHub Pages
- Aceite:
  - janela aprovada visualmente

## 4) Modal padrao propria (substituicao gradual)

- Problema: dialogos ainda podem evoluir para um componente interno unico.
- Itens:
  - concluir implementacao da modal padrao reutilizavel
  - migrar chamadas remanescentes de mensagens para a modal padrao
- Aceite:
  - padrao unico de dialogos no sistema

## 5) Trilha SQLite (fases pendentes)

- Problema: migracao ainda nao concluida ponta a ponta.
- Itens:
  - fechar fases 3-5 do plano
  - endurecer migracao JSON -> SQLite
  - validar rollback e backup automatico
- Aceite:
  - runtime estavel com SQLite para configuracoes/configuracoes

## 6) Revisao de defaults de configuracao

- Itens candidatos:
  - revisar defaults uteis por ferramenta (Harvest, Organizer, Notes)
  - padronizar valores iniciais para reduzir setup manual
- Aceite:
  - primeira execucao mais orientada, com menos campos vazios

