# Configuracoes Atuais das Ferramentas (Runtime + Persistencia)

## Objetivo
Inventario completo das propriedades que compoem cada ferramenta no runtime:
- campos de execucao (`*Request`)
- campos obrigatorios/opcionais (UI + validadores)
- configuracao global (ConfigService)
- configuracao por item (configuracao/projeto/conexao)
- persistencia local (`settings.json`, `appsettings.json`/SQLite, configuracoes, ngrok store)

Baseado no estado atual do codigo em `src/Tools` e `src/Presentation/DevTools.Presentation.Wpf`.

## Camadas de configuracao

1. `Request` da ferramenta (contrato de execucao)
- Fica em `src/Tools/DevTools.<Tool>/Models/*Request.cs`.
- E o payload efetivo para o engine rodar.

2. `SettingsService` (estado local de UI)
- Arquivo: `%AppData%/DevTools/settings.json`.
- Guarda ultimo valor usado, posicao de janela etc.
- Nao e configuracao de negocio global.

3. `ConfigService` (configuracao global)
- Backend selecionavel por `DEVTOOLS_STORAGE_BACKEND`:
  - `json`: `appsettings.json` na pasta do executavel
  - `sqlite`: tabela `AppSettings` no banco SQLite do DevTools
- Secoes globais atuais: `Harvest`, `Organizer`, `Migrations`, `Notes`, `GoogleDrive`.

4. `ToolConfigurationManager` (configuracao por item)
- Arquivos: `%AppData%/DevTools/configurations/devtools.<tool>.json`
- Modelo: `ToolConfiguration { Name, IsDefault, Options<chave,valor> }`
- Conceitualmente e lista de configuracoes nomeadas (configuracao/projeto/conexao/tunel, conforme ferramenta).

5. Ngrok store proprio
- Independente do `ConfigService`.
- JSON: `%AppData%/DevTools/ngrok.settings.json`
- ou SQLite (`NgrokSettings`) via `DEVTOOLS_STORAGE_BACKEND=sqlite`.

---

## 1) Harvest

### Contrato de execucao
`HarvestRequest`:
- `RootPath` (obrigatorio)
- `OutputPath` (opcional no modelo; UI hoje exige)
- `ConfigPath` (opcional)
- `MinScore` (opcional, >= 0)
- `CopyFiles` (bool)

### Configuracao global
Secao `Harvest` (`HarvestConfig`):
- `Rules.Extensions`
- `Rules.ExcludeDirectories`
- `Rules.IgnoreUsingPrefixes`
- `Rules.MaxFileSizeKb`
- `Weights.*`
- `Categories[]`
- `MinScoreDefault`
- `TopDefault`

Defaults da tool:
- embutidos em `HarvestConfig.json`
- fallback de diretorios em `HarvestDefaults.DefaultExcludeDirectories`

### Persistencia local de UI
`settings.json`:
- `LastHarvestSourcePath`
- `LastHarvestOutputPath`
- `LastHarvestConfigPath`
- `LastHarvestMinScore`
- `LastHarvestCopyFiles`

### Configuracao/projeto nomeado
- Nao consumido na janela de execucao atual.

---

## 2) Organizer

### Contrato de execucao
`OrganizerRequest`:
- `InboxPath` (obrigatorio)
- `OutputPath` (opcional; se vazio usa `InboxPath`)
- `ConfigPath` (opcional)
- `MinScore` (opcional)
- `Apply` (bool; false = simulacao)

### Configuracao global
Secao `Organizer` (`OrganizerConfig`):
- `AllowedExtensions[]`
- `MinScoreDefault`
- `FileNameWeight`
- `DeduplicateByHash`
- `DeduplicateByName`
- `DeduplicateFirstLines`
- `Categories[]` com:
  - `Name`, `Folder`
  - `Keywords[]`, `NegativeKeywords[]`
  - `KeywordWeight`, `NegativeWeight`, `MinScore`

### Regras internas relevantes
- Se `ConfigPath` vazio, engine tenta `output/devtools.docs.json`.
- Se nao existir, usa `new OrganizerConfig()` (defaults da classe).

### Persistencia local de UI
`settings.json`:
- `LastOrganizerInputPath`

### Configuracao/projeto nomeado
- Nao consumido na janela de execucao atual.

---

## 3) Migrations

### Contrato de execucao
`MigrationsRequest`:
- `Action` (`AddMigration` | `UpdateDatabase`) obrigatorio
- `Provider` (`SqlServer` | `Sqlite`) obrigatorio
- `Settings` (`MigrationsSettings`) obrigatorio
- `MigrationName` (obrigatorio para `AddMigration`)
- `DryRun` (bool)
- `WorkingDirectory` (opcional)

`MigrationsSettings`:
- `RootPath` (obrigatorio)
- `StartupProjectPath` (obrigatorio)
- `DbContextFullName` (obrigatorio)
- `Targets[]` (obrigatorio para provider selecionado)
- `AdditionalArgs` (opcional; com bloqueio de args de projeto/contexto)

### Configuracao global
Secao `Migrations`:
- espelha `MigrationsSettings` completo.

### Persistencia local de UI
`settings.json`:
- `LastMigrationsRootPath`
- `LastMigrationsStartupPath`
- `LastMigrationsDbContext`
- `MigrationsWindowTop/Left`

### Configuracao/projeto nomeado (ativo)
Ferramenta usa configuracao default de `Migrations` em runtime.
Chaves usadas em `ToolConfiguration.Options`:
- `root-path`
- `startup-path`
- `target-sqlserver-path`
- `target-sqlite-path`
- `dbcontext`
- `additional-args`

---

## 4) Snapshot

### Contrato de execucao
`SnapshotRequest`:
- `RootPath` (obrigatorio)
- `OutputBasePath` (opcional)
- `GenerateText` (bool)
- `GenerateJsonNested` (bool)
- `GenerateJsonRecursive` (bool)
- `GenerateHtmlPreview` (bool)
- `IgnoredDirectories[]` (opcional)
- `MaxFileSizeKb` (opcional, > 0)

Regra obrigatoria:
- pelo menos 1 formato de saida deve estar marcado.

### Configuracao global
- Nao existe secao global dedicada em `ConfigService` para Snapshot.

### Persistencia local de UI
`settings.json`:
- `LastSnapshotRootPath`
- `LastSnapshotOutputBasePath`
- `LastSnapshotIgnoredDirectories`
- `LastSnapshotMaxFileSizeKb`
- `LastSnapshotGenerateText`
- `LastSnapshotGenerateHtml`
- `LastSnapshotGenerateJsonNested`
- `LastSnapshotGenerateJsonRecursive`
- `SnapshotWindowTop/Left`

### Configuracao/projeto nomeado (ativo)
Ferramenta usa configuracao default de `Snapshot` em runtime.
Chaves:
- `project-path`
- `output-base-path`
- `ignored-directories`
- `max-file-size-kb`
- `generate-text`
- `generate-html`
- `generate-json-nested`
- `generate-json-recursive`

---

## 5) SearchText

### Contrato de execucao
`SearchTextRequest`:
- `RootPath` (obrigatorio)
- `Pattern` (obrigatorio)
- `UseRegex`, `CaseSensitive`, `WholeWord`
- `IncludeGlobs[]`, `ExcludeGlobs[]`
- `MaxFileSizeKb` (opcional, > 0)
- `SkipBinaryFiles` (bool)
- `MaxMatchesPerFile` (>= 0)
- `ReturnLines` (bool)

### Configuracao global
- Nao existe secao global dedicada.

### Defaults internos
- `SearchTextDefaults.DefaultExcludeGlobs` (usado pela UI quando nao ha valor salvo).

### Persistencia local de UI
`settings.json`:
- `LastSearchTextRootPath`
- `LastSearchTextInclude`
- `LastSearchTextExclude`
- `LastSearchTextWholeWord`
- `LastSearchTextSkipBinaryFiles`
- `LastSearchTextReturnLines`
- `LastSearchTextMaxFileSizeKb`
- `LastSearchTextMaxMatchesPerFile`
- `SearchTextWindowTop/Left`

### Configuracao/projeto nomeado
- Infra de chaves existe no `ToolConfigurationUIService` (`root-path`, `search-pattern`, `include`, `exclude`), mas nao esta conectada ao runtime desta janela hoje.

---

## 6) Rename

### Contrato de execucao
`RenameRequest`:
- `RootPath` (obrigatorio)
- `OldText` (obrigatorio)
- `NewText` (obrigatorio)
- `Mode` (`General` | `NamespaceOnly`)
- `DryRun`
- `IncludeGlobs[]`, `ExcludeGlobs[]`
- `BackupEnabled`
- `WriteUndoLog`
- `UndoLogPath`
- `ReportPath`
- `MaxDiffLinesPerFile` (> 0)

### Configuracao global
- Nao existe secao global dedicada.

### Persistencia local de UI
`settings.json`:
- `LastRenameRootPath`
- `LastRenameInclude`
- `LastRenameExclude`
- `LastRenameDryRun`
- `LastRenameUndoLogPath`
- `LastRenameReportPath`
- `LastRenameMaxDiffLinesPerFile`
- `RenameWindowTop/Left`

### Configuracao/projeto nomeado
- Infra de chaves existe no `ToolConfigurationUIService` (`old-text`, `new-text`, `include`, `exclude`), mas nao esta conectada ao runtime desta janela hoje.

---

## 7) SSH Tunnel

### Contrato de execucao da tool
`SshTunnelRequest`:
- `Action` (`Start` | `Stop` | `Status`)
- `Configuration` (`TunnelConfiguration`) obrigatorio para `Start`

`TunnelConfiguration`:
- `Name`
- `SshHost`, `SshPort`, `SshUser`
- `IdentityFile`
- `LocalBindHost`, `LocalPort`
- `RemoteHost`, `RemotePort`
- `StrictHostKeyChecking`
- `ConnectTimeoutSeconds`

### Runtime da janela
- Monta `TunnelConfiguration` direto da UI e chama `TunnelService.StartAsync`.
- Nome hoje e derivado automaticamente via `BuildTunnelName()`.

### Configuracao global
- Nao existe secao global ativa para SSH tunnel.

### Persistencia local de UI
`settings.json`:
- `LastSshStrictHostKeyChecking`
- `LastSshConnectTimeoutSeconds`
- `SshWindowTop/Left` (sem uso efetivo hoje para reposicionamento)

### Configuracao/projeto nomeado
- Ferramenta tem tela de configuracao por item (terminologia de tunel) e chaves no `ToolConfigurationUIService`:
  - `ssh-configuration-name`, `ssh-host`, `ssh-port`, `ssh-user`
  - `identity-file`
  - `strict-host-key-checking`, `connect-timeout-seconds`
  - `local-bind`, `local-port`, `remote-host`, `remote-port`
- Observacao: a janela de execucao SSH atual nao carrega esse configuracao automaticamente.

---

## 8) Ngrok

### Contrato de execucao da tool
`NgrokRequest`:
- `Action` (`ListTunnels`, `CloseTunnel`, `StartHttp`, `KillAll`, `Status`)
- `BaseUrl` (default `http://127.0.0.1:4040/`)
- `TimeoutSeconds` (> 0)
- `RetryCount` (>= 0)
- `TunnelName` (obrigatorio para `CloseTunnel`)
- `StartOptions` (obrigatorio para `StartHttp`):
  - `Protocol` (`http|https`)
  - `Port`
  - `ExecutablePath` (opcional)
  - `ExtraArgs[]` (opcional)

### Runtime da janela
- Usa `NgrokSetupService` (config + ambiente + start/stop/list).
- Campos operacionais:
  - onboarding token
  - porta local para iniciar tunel
- Bloqueia start se ngrok nao instalado/configurado.

### Configuracao global
- Nao usa `ConfigService` para Ngrok.
- Usa store proprio (`NgrokSettings`):
  - `ExecutablePath`
  - `AuthToken`
  - `AdditionalArgs`

### Persistencia local de UI
`settings.json`:
- `NgrokWindowTop/Left`

### Configuracao/projeto nomeado
- Existe cadastro de conexoes nomeadas na tela de configuracoes (chaves no `ToolConfigurationUIService`), mas a janela Ngrok runtime atual usa `NgrokSetupService`/`NgrokSettings`, nao `ToolConfigurationManager`.

---

## 9) Notes

### Contrato de execucao
`NotesRequest` (por `Action`):
- base:
  - `Action`
  - `NoteKey`
  - `Content`
  - `NotesRootPath`
  - `ConfigPath`
  - `Overwrite`
- modo simples:
  - `Title`
  - `LocalDate`
  - `OutputPath`
  - `ZipPath`
  - `UseMarkdown`
  - `CreateDateFolder`

Acoes:
- `LoadNote`
- `SaveNote`
- `CreateItem`
- `ListItems`
- `ExportZip`
- `ImportZip`

### Configuracao global
Secao `Notes`:
- `StoragePath`
- `DefaultFormat` (`.txt|.md`)
- `AutoCloudSync`
- `InitialListDisplay` (`Auto|8|15|20`)

Secao `GoogleDrive`:
- `IsEnabled`
- `ClientId`
- `ClientSecret`
- `ProjectId`
- `FolderName`

### Persistencia local de UI
`settings.json`:
- `NotesStoragePath`
- geometria da janela de notas (`NotesWindowTop/Left/Width/Height`)

### Regras de runtime relevantes
- Sempre resolve pasta fisica de notas.
- `Save` exige titulo e conteudo.
- Extensao de arquivo respeita combo (`.txt`/`.md`) para nova nota.
- Se `AutoCloudSync=true` e Google Drive valido, faz upload apos salvar local.

---

## 10) Image Splitter

### Contrato de execucao
`ImageSplitRequest`:
- `InputPath` (obrigatorio)
- `OutputDirectory` (opcional)
- `OutputBaseName` (opcional)
- `OutputExtension` (opcional, deve comecar com `.`)
- `AlphaThreshold` (byte)
- `StartIndex` (>= 1)
- `Overwrite`
- `MinRegionWidth` (>= 1)
- `MinRegionHeight` (>= 1)

### Configuracao global
- Nao existe secao global dedicada.

### Persistencia local de UI
`settings.json`:
- `LastImageSplitInputPath`
- `LastImageSplitOutputDir`
- `ImageSplitWindowTop/Left`

---

## 11) UTF8 Convert

### Contrato de execucao
`Utf8ConvertRequest`:
- `RootPath` (obrigatorio)
- `Recursive`
- `DryRun`
- `CreateBackup`
- `OutputBom`
- `IncludeGlobs[]`
- `ExcludeGlobs[]`

### Configuracao global
- Nao existe secao global dedicada.

### Persistencia local de UI
`settings.json`:
- `LastUtf8RootPath`
- `LastUtf8DryRun`
- `LastUtf8IncludeGlobs`
- `LastUtf8ExcludeGlobs`
- `Utf8WindowTop/Left`

---

## Resumo objetivo (para definicao)

Estado atual de "global vs por item":
- Global forte: `Harvest`, `Organizer`, `Migrations`, `Notes`, `GoogleDrive`.
- Por item (ToolConfigurationManager): `Migrations` e `Snapshot` efetivamente usados no runtime; `SSHTunnel` e `Ngrok` com cadastro disponivel, mas runtime ainda nao consumindo esse cadastro.
- Somente estado local (ultimo uso): `Rename`, `SearchText`, `ImageSplitter`, `Utf8Convert`, `Harvest` (alem da global), `Organizer` (entrada), partes de `Migrations/Snapshot/SSH/Ngrok`.

Se quiser, o proximo passo e eu gerar a versao 2 deste documento em formato de matriz unica (ferramenta x propriedade x escopo x obrigatoriedade x armazenamento), pronta para virar checklist de refatoracao.


