## Mapeamento de Componentes WPF por Tela

Este documento descreve os principais componentes de cada janela WPF do DevTools, com foco em:

- ID do componente (`x:Name`)
- Tipo (TextBox, ComboBox, UserControl etc.)
- O que o componente exibe para o usuÃ¡rio
- Que tipo de valor ele espera / representa no cÃ³digo

O objetivo Ã© facilitar conversas como â€œajustar o combo da tela Xâ€ sem dÃºvida sobre qual controle estÃ¡ sendo usado.

---

## ConvenÃ§Ãµes gerais

- **PathSelector**: controle de seleÃ§Ã£o de caminho (pasta ou arquivo).
- **ModernTextBoxStyle / ModernComboBoxStyle**: estilos globais definidos em `Theme/DarkTheme.xaml`.
- Em geral os campos exibem **string** e o cÃ³digo interpreta como:
  - Caminho (arquivo/pasta)
  - NÃºmero (porta, tamanho, peso)
  - Flag (booleana)
  - Identificador lÃ³gico (nome de configuracao, categoria, migration etc.)

---

## SshTunnelWindow â€“ TÃºnel SSH

Arquivo: `Views/SshTunnelWindow.xaml`

| ID                 | Tipo            | Exibe                                   | Espera / representa                                               |
| ------------------ | --------------- | --------------------------------------- | ----------------------------------------------------------------- |
| SshHostInput       | TextBox         | Host do bastion                         | Hostname/IP usado na conexÃ£o SSH                                  |
| SshPortInput       | TextBox         | Porta do bastion                        | Porta numÃ©rica do bastion                                         |
| SshUserInput       | TextBox         | UsuÃ¡rio SSH                             | UsuÃ¡rio passado para o comando SSH                                |
| IdentityFileInput  | TextBox         | Caminho do arquivo de chave privada     | Caminho de arquivo de chave                                       |
| LocalBindInput     | TextBox         | Host local (bind)                       | Host/IP local da regra de tÃºnel                                   |
| LocalPortInput     | TextBox         | Porta local                             | Porta numÃ©rica local                                              |
| RemoteHostInput    | TextBox         | Host remoto (destino)                   | Host/IP remoto                                                    |
| RemotePortInput    | TextBox         | Porta remota                            | Porta numÃ©rica remota                                             |
| StatusIndicator    | Ellipse         | Cor de status (â€œParadoâ€, â€œConectadoâ€)   | Estado atual do tÃºnel (preenchido pelo codeâ€‘behind)               |
| StatusText         | TextBlock       | Texto de status                         | String de status                                                  |
| ToggleTunnelButton | Button          | â€œConectarâ€ / â€œDesconectarâ€              | Ao clicar, lÃª todos os inputs e inicia/encerra o tÃºnel            |

---

## MigrationsWindow â€“ Migrations EF Core

Arquivo: `Views/MigrationsWindow.xaml`

| ID                | Tipo          | Exibe                                   | Espera / representa                                               |
| ----------------- | ------------- | --------------------------------------- | ----------------------------------------------------------------- |
| ProjectSelector   | PathSelector  | Pasta raiz do projeto (.csproj)        | Caminho de pasta                                                  |
| StartupSelector   | PathSelector  | Pasta do projeto de startup            | Caminho de pasta                                                  |
| ActionCombo       | ComboBox      | â€œAdd Migrationâ€ / â€œUpdate Databaseâ€    | Tag = Add/Update; define comando `dotnet ef`                      |
| ProviderCombo     | ComboBox      | â€œSQL Serverâ€ / â€œSQLiteâ€                | Tag = SqlServer/Sqlite; define provider                           |
| MigrationNameInput| TextBox       | Nome da migration                      | Identificador da migration                                        |
| DbContextInput    | TextBox       | Full name do DbContext                 | String usada como `--context`                                     |
| DryRunCheck       | CheckBox      | â€œDry Run (Gerar comando sem executar)â€ | Booleano; gera comando sem executar                               |
| OutputText        | TextBox (RO)  | Log de saÃ­da                           | Texto do comando `dotnet ef`                                      |

---

## NgrokWindow â€“ Expor Porta (Ngrok)

Arquivo: `Views/NgrokWindow.xaml`

| ID               | Tipo            | Exibe                                   | Espera / representa                                               |
| ---------------- | --------------- | --------------------------------------- | ----------------------------------------------------------------- |
| PortInput        | TextBox         | Porta local (ex.: 5000)                 | Porta numÃ©rica local                                              |
| StartButton      | Button          | â€œExpor Portaâ€                           | Dispara criaÃ§Ã£o do tÃºnel Ngrok                                    |
| TunnelsList      | ListBox         | Lista de tÃºneis ativos                  | Items com PublicUrl e Config.Addr via binding                     |
| EmptyStateText   | TextBlock       | â€œNenhum tÃºnel ativo.â€                   | Mensagem quando a lista estÃ¡ vazia                                |

---

## SearchTextWindow â€“ Buscar Texto

Arquivo: `Views/SearchTextWindow.xaml`

| ID                   | Tipo          | Exibe                              | Espera / representa                                      |
| -------------------- | ------------- | ---------------------------------- | -------------------------------------------------------- |
| PathSelector         | PathSelector  | DiretÃ³rio de busca                 | Pasta raiz para varrer arquivos                         |
| SearchTextInput      | TextBox       | Texto ou regex                     | String usada como termo de busca                        |
| UseRegexCheck        | CheckBox      | â€œUsar Regexâ€                       | Booleano; alterna entre match literal e regex           |
| CaseSensitiveCheck   | CheckBox      | â€œCase Sensitiveâ€                   | Booleano                                                 |
| IncludePatternInput  | TextBox       | Globs de inclusÃ£o                  | Lista de padrÃµes (ex.: *.cs, *.ts)                      |
| ExcludePatternInput  | TextBox       | Globs de exclusÃ£o                  | Lista de padrÃµes (ex.: bin, obj, node_modules)          |
| OutputText           | TextBox (RO)  | Resultados da busca                | Texto formatado com os matches                          |

---

## OrganizerWindow â€“ Organizer

Arquivo: `Views/OrganizerWindow.xaml`

| ID                 | Tipo            | Exibe                                   | Espera / representa                                               |
| ------------------ | --------------- | --------------------------------------- | ----------------------------------------------------------------- |
| InputPathSelector  | PathSelector    | Pasta de entrada                        | DiretÃ³rio de origem                                               |
| OutputPathSelector | PathSelector    | Pasta de saÃ­da (opcional)               | DiretÃ³rio de destino                                              |
| SimulateCheck      | CheckBox        | â€œSimular (Apenas Teste)â€                | Booleano; se true, nÃ£o move arquivos de fato                      |

---

## HarvestWindow â€“ Harvest

Arquivo: `Views/HarvestWindow.xaml`

| ID                    | Tipo            | Exibe                           | Espera / representa                               |
| --------------------- | --------------- | --------------------------------| ------------------------------------------------- |
| ConfigurationSelector       | ConfigurationSelector | Configuracao de harvest               | ConfiguraÃ§Ãµes completas de anÃ¡lise                |
| SourcePathSelector    | PathSelector    | DiretÃ³rio de origem             | Pasta a ser analisada                             |
| OutputPathSelector    | PathSelector    | DiretÃ³rio de destino            | Pasta de backup                                   |
| ConfigPathSelector    | PathSelector    | Arquivo de configuraÃ§Ã£o         | Caminho de arquivo de config                      |
| MinScoreBox           | TextBox         | PontuaÃ§Ã£o mÃ­nima                | NÃºmero (double/int)                               |
| CopyFilesCheck        | CheckBox        | â€œCopiar Arquivosâ€               | Booleano                                          |
| RememberSettingsCheck | CheckBox        | â€œLembrar configuraÃ§Ãµesâ€         | Booleano                                          |

---

## RenameWindow â€“ Renomear em Massa

Arquivo: `Views/RenameWindow.xaml`

| ID                    | Tipo            | Exibe                           | Espera / representa                               |
| --------------------- | --------------- | --------------------------------| ------------------------------------------------- |
| ConfigurationSelector       | ConfigurationSelector | Configuracao de rename                | ConfiguraÃ§Ãµes salvas de rename                    |
| RootPathSelector      | PathSelector    | Pasta raiz                      | DiretÃ³rio onde o rename roda                      |
| OldTextBox            | TextBox         | Texto antigo                    | String alvo                                       |
| NewTextBox            | TextBox         | Texto novo                      | String de substituiÃ§Ã£o                            |
| ModeCombo             | ComboBox        | Modo de rename                  | Algoritmo (geral, namespace inteligente)          |
| IncludeBox            | TextBox         | Globs de inclusÃ£o               | Lista de padrÃµes                                  |
| ExcludeBox            | TextBox         | Globs de exclusÃ£o               | Lista de padrÃµes                                  |
| BackupCheck           | CheckBox        | â€œCriar Backupâ€                  | Booleano                                          |
| UndoLogCheck          | CheckBox        | â€œLog de ReversÃ£oâ€               | Booleano                                          |
| DryRunCheck           | CheckBox        | â€œDry Run (Teste)â€               | Booleano; simula sem alterar arquivos             |
| RememberSettingsCheck | CheckBox        | â€œLembrarâ€                       | Booleano; persiste Ãºltima configuraÃ§Ã£o            |

---

## SnapshotWindow â€“ Snapshot

Arquivo: `Views/SnapshotWindow.xaml`

| ID                 | Tipo            | Exibe                           | Espera / representa                               |
| ------------------ | --------------- | --------------------------------| ------------------------------------------------- |
| ConfigurationSelector    | ConfigurationSelector | Configuracao de snapshot              | Configuracao com paths e formatos                       |
| RootPathSelector   | PathSelector    | Pasta raiz do projeto           | DiretÃ³rio base do snapshot                        |
| TextCheck          | CheckBox        | â€œTexto Unificado (.txt)â€        | Booleano                                          |
| HtmlCheck          | CheckBox        | â€œPreview HTML (.html)â€          | Booleano                                          |
| JsonNestedCheck    | CheckBox        | â€œJSON Aninhado (.json)â€         | Booleano                                          |
| JsonRecursiveCheck | CheckBox        | â€œJSON Recursivo (.json)â€        | Booleano                                          |


