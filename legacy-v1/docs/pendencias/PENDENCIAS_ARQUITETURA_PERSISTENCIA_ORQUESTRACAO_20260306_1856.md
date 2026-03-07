# PENDENCIAS - Arquitetura de Persistencia e Orquestracao

- Status: Concluida
- Responsavel: Equipe DevTools
- Criado em: 2026-03-06 18:56
- Atualizado em: 2026-03-06 20:39
- Relacionado a: `docs/definicoes/DEFINICAO_ARQUITETURA_PERSISTENCIA_ORQUESTRACAO_20260306_1856.md`

## Fase 0 - Preparacao e alinhamento
- [x] Consolidar definicao tecnica no contexto real do repositorio.
- [x] Validar principios arquiteturais com foco em separacao Host/Tools/Core/Persistencia.
- [x] Registrar demanda com trilha completa (definicao + pendencias + log + registro mestre).

## Fase 1 - Inventario tecnico do estado atual
- [x] Mapear tudo que hoje estava em `Presentation.Wpf/Persistence`.
- [x] Classificar por tipo: `DbContext`, entidades, stores, bootstrap, interfaces.
- [x] Identificar dependencias diretas dessas classes nas telas/servicos da WPF.
- [x] Catalogar pontos de persistencia por ferramenta (configuracao nomeada, settings, notes, ngrok).

## Fase 2 - Desenho do alvo de infraestrutura
- [x] Definir estrutura do projeto `Infrastructure` e subpastas oficiais.
- [x] Definir `DbContext` unico alvo e fronteiras de responsabilidade.
- [x] Definir politica de stores (o que permanece, o que migra, o que descontinua).
- [x] Definir estrategia unica de migrations centralizadas.

## Fase 3 - Plano de migracao incremental
- [x] Sequenciar migracao por blocos tecnicos (bootstrap, contexto, stores, entidades).
- [x] Definir estrategia de compatibilidade temporaria durante a transicao.
- [x] Definir ordem de remocao segura de codigo legado em `Presentation.Wpf/Persistence`.
- [x] Definir rollback tecnico por etapa.

## Fase 4 - Execucao tecnica
- [x] Criar projeto de infraestrutura e mover componentes de persistencia.
- [x] Reapontar injecoes/servicos para nova camada.
- [x] Remover dependencia direta de persistencia no host WPF.
- [x] Garantir fluxo WPF -> Tool -> Repository/Store -> Infrastructure.

## Fase 5 - Validacao e fechamento
- [x] Executar build completo da solucao.
- [x] Executar testes automatizados e registrar resultado.
- [x] Revisar documentacao tecnica (arquitetura e definicoes relacionadas).
- [x] Marcar demanda como concluida no registro mestre.

## Fase 6 - Dominio por ferramenta (entidades e repositorios)
- [x] Definir recorte por ferramenta para mover entidades de dominio persistente para cada `src/Tools/DevTools.<Tool>/`.
- [x] Definir contrato de repositorio por ferramenta (`Repositories`) sem acoplamento ao host WPF.
- [x] Implementar primeira rodada (Notes, configuracoes nomeadas e metadados) com uso de Infrastructure apenas como detalhe tecnico.
  - [x] Notes: criar `Repositories/INotesItemsRepository` e `Repositories/NotesItemsRepository`.
  - [x] Notes: mover exclusao de nota para dominio (`NotesAction.DeleteItem`) e remover exclusao manual em WPF.
  - [x] Metadados Notes: alinhar `Json/SqliteNoteMetadataStore` ao contrato de dominio (`INoteMetadataRepository`, `NoteMetadataEntity`) e remover tipos legados.
  - [x] Configuracoes nomeadas: repositorio de dominio dedicado (`IToolConfigurationRepository` + `ToolConfigurationRepository`), com `ToolConfigurationManager` usando repositorio.
- [x] Ajustar testes para validar fluxo `Host -> Tool -> Repository -> Infrastructure`.

## Criterios de conclusao
- [x] Camada WPF sem `DbContext` e sem persistencia de negocio.
- [x] Persistencia consolidada em camada de infraestrutura dedicada.
- [x] Sem regressao funcional nas ferramentas principais.
- [x] Documentacao e logs atualizados com evidencias.
- [x] Entidades e repositorios orientados por ferramenta implementados conforme arquitetura alvo.

