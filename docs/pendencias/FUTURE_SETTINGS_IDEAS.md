# Roadmap de Evolucao de Configuracoes (Futuro)

Este arquivo lista ideias de melhoria para proximas versoes.

## 1. Painel de saude por ferramenta

- SSH: quantidade de perfis e ultimo perfil usado
- Ngrok: status (instalado/configurado/ativo)
- Notes: caminho ativo + status de sync
- Storage: backend atual (JSON/SQLite)

## 2. Automacoes de preenchimento

- detector de DbContext no painel de Migrations
- detector de executaveis (`ngrok.exe`, `ssh.exe`)
- sugestao de nomes/padroes para exportacoes e snapshots

## 3. Melhorias de UX de formulario

- chips/tags para listas de extensoes e pastas ignoradas
- validacao inline com icone e mensagem contextual
- padronizacao final de microcopy dos botoes (novo/salvar/remover)

## 4. Perfil operacional

- export/import consolidado de perfis e configuracoes
- sincronizacao opcional de perfis entre maquinas

## 5. Observabilidade

- painel unico de status no shell (jobs, tuneis, storage, logs)
