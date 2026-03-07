# Roadmap de Evolucao de Configuracoes (Futuro)

Este arquivo lista ideias de melhoria para proximas versoes.

## 1. Painel de saude por ferramenta

- SSH: quantidade de configuracoes e ultimo configuracao usado
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

## 4. Configuracao operacional

- export/import consolidado de configuracoes e configuracoes
- sincronizacao opcional de configuracoes entre maquinas

## 5. Observabilidade

- painel unico de status no shell (jobs, tuneis, storage, logs)

## 6. Atualizacao de dependencias (fechamento de release)

- executar upgrades de pacotes somente no fim, apos estabilizacao funcional
- atualizar em lotes pequenos (testes primeiro, runtime depois)
- validar `dotnet build` e `dotnet test` a cada lote
- tratar migracao de `xunit` v2 (`Legacy`) para `xunit.v3` em etapa dedicada

