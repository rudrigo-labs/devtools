# LOG DEMANDA - Refatoracao das Entidades e Propriedades das Ferramentas

- Demanda relacionada: `DEFINICAO_REFATORACAO_ENTIDADES_FERRAMENTAS_20260306_1315.md`
- Status: Em andamento
- Responsavel: Equipe DevTools
- Iniciado em: 2026-03-06 13:15
- Atualizado em: 2026-03-06 18:56

## Entradas de log
- [2026-03-06 13:15] Demanda iniciada com pacote completo de documentacao.
- [2026-03-06 13:15] Estrategia definida: execucao por fases com checklist por fase.
- [2026-03-06 13:15] Escopo inicial confirmado: refatoracao de entidades/propriedades de configuracao por ferramenta.
- [2026-03-06 13:42] Fase 0 concluida com baseline tecnico registrado em relatorio dedicado.
- [2026-03-06 13:42] Fase 1 concluida com definicao de modelo unificado, matriz de obrigatoriedade e nomenclatura final.
- [2026-03-06 13:55] Fase 2 executada parcialmente com contrato unificado implementado e estrategia de migracao definida.
- [2026-03-06 13:55] Fase 3 iniciada com infraestrutura comum aplicada (mapper/metadata/normalizacao) para ferramentas com configuracoes nomeadas.
- [2026-03-06 13:56] Validacao tecnica da rodada: `dotnet build` WPF OK e `dotnet test` DevTools.Tests OK (36 aprovados, 2 ignorados).
- [2026-03-06 14:35] Remocao da nomenclatura antiga aplicada no codigo fonte: classes, services, stores, XAML e fluxo SSH migrados para `Configuration`.
- [2026-03-06 14:35] Renomeacao de arquivos concluida (`ToolConfiguration*` e `*ToolConfigurationStore`) para eliminar residuos de nomenclatura antiga.
- [2026-03-06 14:35] Validacao tecnica apos refatoracao: build e testes verdes (`36 aprovados`, `2 ignorados`).
- [2026-03-06 15:07] Fase 3 estendida para ferramentas restantes de execucao: `Harvest`, `Organizer`, `SearchText`, `Rename`, `ImageSplitter`, `Utf8Convert`.
- [2026-03-06 15:07] Configuracoes nomeadas habilitadas no painel de configuracoes (cards e formularios dinamicos para ferramentas restantes).
- [2026-03-06 15:07] Janelas de execucao atualizadas para carregar configuracao padrao por ferramenta ao abrir.
- [2026-03-06 15:07] Validacao tecnica da rodada: `dotnet build` WPF OK e `dotnet test` DevTools.Tests OK (`36 aprovados`, `2 ignorados`).
- [2026-03-06 15:28] Fase 4 item 1 executado: validacao obrigatoria com realce inline aplicada nas janelas de execucao (`Harvest`, `SearchText`, `Rename`, `Organizer`, `Snapshot`, `ImageSplitter`, `Utf8Convert`, `SSH`, `Migrations`, `Ngrok`).
- [2026-03-06 15:28] Fase 4 item 4 executado: `dotnet build` WPF e `dotnet test` DevTools.Tests concluídos com sucesso (`36 aprovados`, `2 ignorados`).
- [2026-03-06 18:56] Definicao consolidada de arquitetura/persistencia criada com base em textos de referencia e contexto real do projeto: `docs/definicoes/DEFINICAO_ARQUITETURA_PERSISTENCIA_ORQUESTRACAO_20260306_1856.md`.

## Mudancas de escopo
- Nenhuma ate o momento.

## Evidencias
- Definicao criada.
- Pendencias por fase criada.
- Registro mestre atualizado.
- Baseline tecnico: `docs/relatorios/BASELINE_TECNICA_REFATORACAO_ENTIDADES_20260306_1342.md`
- Definicao fase 1: `docs/definicoes/DEFINICAO_MODELO_UNIFICADO_CONFIGURACOES_FASE1_20260306_1342.md`
- Definicao fase 2: `docs/definicoes/DEFINICAO_PERSISTENCIA_E_MIGRACAO_FASE2_20260306_1342.md`
- Definicao consolidada arquitetura/persistencia: `docs/definicoes/DEFINICAO_ARQUITETURA_PERSISTENCIA_ORQUESTRACAO_20260306_1856.md`
- Eliminacao de nomenclatura antiga validada em `src` (sem ocorrencias no dominio das ferramentas; mantido apenas identificador do sistema operacional para pasta do usuario).

## Proximos passos
1. Concluir o item de `Notes` na Fase 3 (pontos estritamente desta demanda).
2. Revisar Fase 4 itens 2 e 3 (`mensagens/comportamento de erro` e `compatibilidade de configuracoes antigas`).
3. Avancar para Fase 5 de fechamento.

