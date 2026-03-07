# Pendencias de Configuracoes - 20260305_2309

## Objetivo
Consolidar as pendencias de configuracoes identificadas na varredura completa das ferramentas, para execucao faseada sem nova varredura geral.

## Escopo
- Apenas itens de configuracao (UI de configuracao, configuration/config, defaults e consistencia Tool x WPF).
- Nao inclui ajustes visuais gerais fora de configuracao.

## Prioridade P0 (bloqueia consistencia/fluxo)

### Migrations
- [x] Alinhar acoes da UI com o engine (remover `Remove Migration` da tela ou implementar no engine).
- [x] Alinhar providers da UI com o enum real (hoje enum suporta `SqlServer` e `Sqlite`; UI mostra `PostgreSQL`).
- [x] Corrigir seletores de caminho para comportamento de arquivo/projeto conforme validacao real do engine.
- [x] Garantir que `MigrationsSettings.Targets` seja configurado/salvo pela configuracao (hoje validator exige e a UI de execucao nao monta).
- [x] Tornar `AdditionalArgs` opcional na configuracao (hoje esta sendo exigido na tela, mas o modelo permite vazio).

### Organizer
- [x] Corrigir regra de "Pasta de Saida (Opcional)" para realmente opcional no fluxo da janela (hoje validacao exige preenchimento).

### ImageSplit
- [x] Corrigir regra de "Pasta de Saida (Opcional)" para realmente opcional no fluxo da janela (engine ja tem fallback).

### Validacoes (UI configuracoes)
- [x] Implementar validacao inline para campos obrigatorios (sem depender apenas de dialog/modal).
- [x] Regra global: botao `Remover` so habilita para objeto ja salvo/existente (desabilitado para item novo pendente).

## Prioridade P1 (lacunas de configuracao)

### Snapshot
- [x] Tratar Snapshot como `Projeto` (nao `Configuracao`) na tela de configuracao/configuracoes.
- [x] Expor em configuracao os campos ja suportados pela Tool: `OutputBasePath`, `IgnoredDirectories`, `MaxFileSizeKb`.
- [x] Permitir defaults de formatos de saida (txt/json/html) via configuracao/config.
- [x] Remover duplicacao de listas de extensoes entre `SnapshotDefaults` e `SnapshotHtmlWriter`.
- [x] Definir estrategia para assets do HTML preview (CDN/offline/cache).

### SearchText
- [x] Expor opcoes existentes no request e nao expostas na UI:
  - [x] `WholeWord`
  - [x] `MaxFileSizeKb`
  - [x] `SkipBinaryFiles`
  - [x] `MaxMatchesPerFile`
  - [x] `ReturnLines`
- [x] Definir defaults editaveis de include/exclude para alinhar com `SearchTextDefaults`.

### Utf8Convert
- [x] Expor opcoes existentes no request e nao expostas na UI:
  - [x] `DryRun`
  - [x] `IncludeGlobs`
  - [x] `ExcludeGlobs`

### Rename
- [x] Expor opcoes avancadas ja suportadas no request:
  - [x] `UndoLogPath`
  - [x] `ReportPath`
  - [x] `MaxDiffLinesPerFile`

### SSHTunnel
- [x] Expor opcoes de configuracao ja suportadas no model:
  - [x] `StrictHostKeyChecking`
  - [x] `ConnectTimeoutSeconds`
- [ ] Trocar nomenclatura de `Configuracao` para `Tunel`/`Nome do Tunel` no fluxo de configuracao.
- [ ] Suportar lista de tuneis nomeados (multiplas configuracoes), sem conceito de configuracao padrao.
- [ ] Permitir abrir/editar/remover tuneis nomeados na configuracao.

## Prioridade P2 (higiene e manutencao de defaults)

### UI das Janelas de Ferramentas
- [x] Verificar ajuste do botao secundario para manter contraste correto com o rodape (evitar mesma cor de fundo do rodape).
- [x] Verificar checkboxes nas telas de configuracao e configuracoes com espacamento vertical/horizontal excessivo e padronizar com o restante da UI.

### Harvest
- [x] Centralizar defaults de exclusao para evitar drift entre:
  - Tool (`HarvestConfig.json`)
  - MainWindow (lista default)
  - Stores de configuracao (JSON e SQLite)
- [x] Definir fonte unica de verdade para defaults de configuracao.

### Organizer (configuracao geral)
- [x] Revisar barra de acao do Organizer (layout da barra, posicao e regras de habilitar/desabilitar por estado do item).
- [x] Expandir tela de configuracao para cobrir campos alem de categorias:
  - [x] `AllowedExtensions`
  - [x] `MinScoreDefault`
  - [x] `FileNameWeight`
  - [x] opcoes de deduplicacao

### Ngrok (modelagem de configuracao)
- [ ] Trocar nomenclatura de `Configuracao` para `Conexao`/`Nome da Conexao` (quando houver cadastro).
- [ ] Suportar lista de conexoes nomeadas (multiplas configuracoes de URL/tunel), sem conceito de configuracao padrao.
- [ ] Evoluir `NgrokSettings` (hoje singleton) para colecao de itens configuraveis.

## Notas de Arquitetura
- Regra-alvo: UI WPF apenas consome a Tool.
- Evitar regra de negocio de configuracao na janela quando ja existe no engine/model.
- Evitar defaults duplicados em mais de um ponto.
- Melhoria proxima versao: substituir persistencia orientada a `configuracao` por persistencia em `listas de configuracoes` por ferramenta, com operacoes de CRUD sobre itens da lista.

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

## Pendencia Adicionada (UX Configuracoes/Projetos)
- [ ] Implementar fluxo de abertura com selecao de configuracao/projeto no clique do card da ferramenta:
  - [ ] Se existir configuracao/projeto salvo, abrir modal com `Selecionar e continuar` e `Seguir sem configuracao`.
  - [ ] Se seguir sem configuracao, mostrar dica com checkbox `Nao exibir novamente`.
  - [ ] Persistir preferencia por ferramenta (nao global).
  - [ ] Permitir reset da preferencia nas configuracoes.
- Referencia detalhada: `docs/pendencias/EXPERIENCIA_CONFIGURACOES_FERRAMENTAS_20260306_0551.md`.

## Pendencia Adicionada (Mudanca Gigante Imediata - Argumentos)
- [ ] Executar desativacao completa de `argumentos adicionais` no fluxo de uso da aplicacao (sem remover codigo legado):
  - [ ] Bloquear exposicao em UI (ferramentas, configuracoes e configuracao/projeto/conexao).
  - [ ] Bloquear gravacao/leitura operacional em persistencia (manter apenas compatibilidade passiva).
  - [ ] Bloquear consumo em runtime (engine/comando final deve ignorar argumentos adicionais).
  - [ ] Manter codigo guardado para futura reativacao controlada (sem exclusao estrutural agora).
- Referencia detalhada: `docs/pendencias/VARREDURA_ARGUMENTOS_FERRAMENTAS_20260306_0943.md`.
- Observacao: item aberto para implementacao imediata na sequencia desta discussao.

## Pendencia Adicionada (Validacao e Documentacao WPF)
- [ ] Verificar se todos os `MessageBox` foram substituidos pelo componente de dialogo padrao do projeto.
- [ ] Criar documento tutorial de WPF para padroes adotados no DevTools.

