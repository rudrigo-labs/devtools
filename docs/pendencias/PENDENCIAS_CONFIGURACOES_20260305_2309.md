# Pendencias de Configuracoes - 20260305_2309

## Objetivo
Consolidar as pendencias de configuracoes identificadas na varredura completa das ferramentas, para execucao faseada sem nova varredura geral.

## Escopo
- Apenas itens de configuracao (UI de configuracao, profile/config, defaults e consistencia Tool x WPF).
- Nao inclui ajustes visuais gerais fora de configuracao.

## Prioridade P0 (bloqueia consistencia/fluxo)

### Migrations
- [x] Alinhar acoes da UI com o engine (remover `Remove Migration` da tela ou implementar no engine).
- [x] Alinhar providers da UI com o enum real (hoje enum suporta `SqlServer` e `Sqlite`; UI mostra `PostgreSQL`).
- [x] Corrigir seletores de caminho para comportamento de arquivo/projeto conforme validacao real do engine.
- [x] Garantir que `MigrationsSettings.Targets` seja configurado/salvo pela configuracao (hoje validator exige e a UI de execucao nao monta).
- [x] Tornar `AdditionalArgs` opcional na configuracao (hoje esta sendo exigido na tela, mas o modelo permite vazio).

### Organizer
- [ ] Corrigir regra de "Pasta de Saida (Opcional)" para realmente opcional no fluxo da janela (hoje validacao exige preenchimento).

### ImageSplit
- [ ] Corrigir regra de "Pasta de Saida (Opcional)" para realmente opcional no fluxo da janela (engine ja tem fallback).

### Validacoes (UI configuracoes)
- [ ] Implementar validacao inline para campos obrigatorios (sem depender apenas de dialog/modal).
- [ ] Regra global: botao `Remover` so habilita para objeto ja salvo/existente (desabilitado para item novo pendente).

## Prioridade P1 (lacunas de configuracao)

### Snapshot
- [x] Tratar Snapshot como `Projeto` (nao `Perfil`) na tela de configuracao/perfis.
- [x] Expor em configuracao os campos ja suportados pela Tool: `OutputBasePath`, `IgnoredDirectories`, `MaxFileSizeKb`.
- [ ] Permitir defaults de formatos de saida (txt/json/html) via perfil/config.
- [ ] Remover duplicacao de listas de extensoes entre `SnapshotDefaults` e `SnapshotHtmlWriter`.
- [ ] Definir estrategia para assets do HTML preview (CDN/offline/cache).

### SearchText
- [ ] Expor opcoes existentes no request e nao expostas na UI:
  - [ ] `WholeWord`
  - [ ] `MaxFileSizeKb`
  - [ ] `SkipBinaryFiles`
  - [ ] `MaxMatchesPerFile`
  - [ ] `ReturnLines`
- [ ] Definir defaults editaveis de include/exclude para alinhar com `SearchTextDefaults`.

### Utf8Convert
- [ ] Expor opcoes existentes no request e nao expostas na UI:
  - [ ] `DryRun`
  - [ ] `IncludeGlobs`
  - [ ] `ExcludeGlobs`

### Rename
- [ ] Expor opcoes avancadas ja suportadas no request:
  - [ ] `UndoLogPath`
  - [ ] `ReportPath`
  - [ ] `MaxDiffLinesPerFile`

### SSHTunnel
- [ ] Expor opcoes de perfil ja suportadas no model:
  - [ ] `StrictHostKeyChecking`
  - [ ] `ConnectTimeoutSeconds`

## Prioridade P2 (higiene e manutencao de defaults)

### UI das Janelas de Ferramentas
- [ ] Verificar ajuste do botao secundario para manter contraste correto com o rodape (evitar mesma cor de fundo do rodape).
- [ ] Verificar checkboxes nas telas de configuracao e perfis com espacamento vertical/horizontal excessivo e padronizar com o restante da UI.

### Harvest
- [ ] Centralizar defaults de exclusao para evitar drift entre:
  - Tool (`HarvestConfig.json`)
  - MainWindow (lista default)
  - Stores de configuracao (JSON e SQLite)
- [ ] Definir fonte unica de verdade para defaults de configuracao.

### Organizer (configuracao geral)
- [ ] Revisar barra de acao do Organizer (layout da barra, posicao e regras de habilitar/desabilitar por estado do item).
- [ ] Expandir tela de configuracao para cobrir campos alem de categorias:
  - [ ] `AllowedExtensions`
  - [ ] `MinScoreDefault`
  - [ ] `FileNameWeight`
  - [ ] opcoes de deduplicacao

## Notas de Arquitetura
- Regra-alvo: UI WPF apenas consome a Tool.
- Evitar regra de negocio de configuracao na janela quando ja existe no engine/model.
- Evitar defaults duplicados em mais de um ponto.

## Ordem sugerida de execucao
1. P0 Migrations
2. P0 Organizer/ImageSplit (opcional real)
3. P1 Snapshot
4. P1 SearchText / Utf8Convert / Rename / SSHTunnel
5. P2 Harvest / Organizer completo

## Status
- Documento criado em: 2026-03-05 23:09 (America/Sao_Paulo)
- Responsavel atual: Time DevTools
- Estado: Aberto

## Pendencia Adicionada (UX Perfis/Projetos)
- [ ] Implementar fluxo de abertura com selecao de perfil/projeto no clique do card da ferramenta:
  - [ ] Se existir perfil/projeto salvo, abrir modal com `Selecionar e continuar` e `Seguir sem perfil`.
  - [ ] Se seguir sem perfil, mostrar dica com checkbox `Nao exibir novamente`.
  - [ ] Persistir preferencia por ferramenta (nao global).
  - [ ] Permitir reset da preferencia nas configuracoes.
- Referencia detalhada: `docs/pendencias/EXPERIENCIA_PERFIS_FERRAMENTAS_20260306_0551.md`.
