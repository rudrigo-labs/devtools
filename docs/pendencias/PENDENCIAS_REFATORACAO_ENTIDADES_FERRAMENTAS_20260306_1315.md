# PENDENCIAS - Refatoracao das Entidades e Propriedades das Ferramentas

- Status: Em andamento
- Responsavel: Equipe DevTools
- Criado em: 2026-03-06 13:15
- Atualizado em: 2026-03-06 14:35
- Relacionado a: `DEFINICAO_REFATORACAO_ENTIDADES_FERRAMENTAS_20260306_1315.md`

## Fase 0 - Preparacao e baseline
- [x] Criar documento de definicao da demanda.
- [x] Criar documento de pendencias por fase.
- [x] Criar log cronologico da demanda.
- [x] Registrar demanda no registro mestre.
- [x] Congelar baseline tecnico para comparacao (build/testes/documentos).

## Fase 1 - Modelo de dados unificado
- [x] Definir estrutura base de configuracao nomeada por ferramenta.
- [x] Definir campos comuns (nome, descricao, ativo, created/updated).
- [x] Definir obrigatoriedade por ferramenta (matriz obrigatorio/opcional).
- [x] Definir nomenclatura final (eliminar ambiguidade de "configuracao/projeto").

## Fase 2 - Persistencia e contratos
- [x] Mapear persistencia atual (settings/config/configuration/sqlite/json) por ferramenta.
- [x] Definir contrato unico de persistencia para configuracoes nomeadas.
- [x] Remover uso de persistencia indevida na apresentacao (quando aplicavel).
- [x] Definir estrategia de migracao de dados existentes.

## Fase 3 - Implementacao incremental por ferramenta
- [x] Aplicar infraestrutura comum (contrato + mapper + metadata compativel).
- [x] Migrations
- [x] Snapshot
- [x] SSH Tunnel
- [x] Ngrok
- [ ] Harvest
- [ ] Organizer
- [ ] SearchText
- [ ] Rename
- [ ] Notes (somente pontos desta demanda)
- [ ] Image Splitter
- [ ] UTF8 Convert

## Fase 4 - Validacoes e hardening
- [ ] Aplicar validacoes obrigatorias de campos.
- [ ] Revisar mensagens e comportamento de erro.
- [ ] Revisar compatibilidade de configuracoes antigas.
- [ ] Executar testes tecnicos por fase.

## Fase 5 - Fechamento
- [ ] Atualizar documentacao tecnica e manual.
- [ ] Consolidar relatorio final da demanda.
- [ ] Marcar demanda como concluida no registro mestre.

## Criterios de conclusao
- [ ] Todas as fases com checklist fechado.
- [ ] Sem regressao critica nas ferramentas principais.
- [ ] Documentacao e registro mestre atualizados.


