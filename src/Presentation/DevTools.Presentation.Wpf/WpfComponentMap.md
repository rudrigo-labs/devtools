## Mapeamento de Componentes WPF por Tela

Este documento descreve os principais componentes de cada janela WPF do DevTools, com foco em:

- ID do componente (`x:Name`)
- Tipo (TextBox, ComboBox, UserControl etc.)
- O que o componente exibe para o usuário
- Que tipo de valor ele espera / representa no código

O objetivo é facilitar conversas como “ajustar o combo da tela X” sem dúvida sobre qual controle está sendo usado.

---

## Convenções gerais

- **ProfileSelector**: controle de perfis reutilizado em várias ferramentas.
- **PathSelector**: controle de seleção de caminho (pasta ou arquivo).
- **ModernTextBoxStyle / ModernComboBoxStyle**: estilos globais definidos em `Theme/DarkTheme.xaml`.
- Em geral os campos exibem **string** e o código interpreta como:
  - Caminho (arquivo/pasta)
  - Número (porta, tamanho, peso)
  - Flag (booleana)
  - Identificador lógico (nome de perfil, categoria, migration etc.)

---

## SshTunnelWindow – Túnel SSH

Arquivo: `Views/SshTunnelWindow.xaml`

| ID                 | Tipo            | Exibe                                   | Espera / representa                                               |
| ------------------ | --------------- | --------------------------------------- | ----------------------------------------------------------------- |
| ProfileSelector    | ProfileSelector | Nome do perfil de túnel SSH             | Nome do perfil → conjunto de configs (host, porta, binds etc.)    |
| SshHostInput       | TextBox         | Host do bastion                         | Hostname/IP usado na conexão SSH                                  |
| SshPortInput       | TextBox         | Porta do bastion                        | Porta numérica do bastion                                         |
| SshUserInput       | TextBox         | Usuário SSH                             | Usuário passado para o comando SSH                                |
| IdentityFileInput  | TextBox         | Caminho do arquivo de chave privada     | Caminho de arquivo de chave                                       |
| LocalBindInput     | TextBox         | Host local (bind)                       | Host/IP local da regra de túnel                                   |
| LocalPortInput     | TextBox         | Porta local                             | Porta numérica local                                              |
| RemoteHostInput    | TextBox         | Host remoto (destino)                   | Host/IP remoto                                                    |
| RemotePortInput    | TextBox         | Porta remota                            | Porta numérica remota                                             |
| StatusIndicator    | Ellipse         | Cor de status (“Parado”, “Conectado”)   | Estado atual do túnel (preenchido pelo code‑behind)               |
| StatusText         | TextBlock       | Texto de status                         | String de status                                                  |
| ToggleTunnelButton | Button          | “Conectar” / “Desconectar”              | Ao clicar, lê todos os inputs e inicia/encerra o túnel            |

---

## MigrationsWindow – Migrations EF Core

Arquivo: `Views/MigrationsWindow.xaml`

| ID                | Tipo          | Exibe                                   | Espera / representa                                               |
| ----------------- | ------------- | --------------------------------------- | ----------------------------------------------------------------- |
| ProfileSelector   | ProfileSelector | Nome do perfil de migrations           | Perfil com paths, provider, contexto etc.                         |
| ProjectSelector   | PathSelector  | Pasta raiz do projeto (.csproj)        | Caminho de pasta                                                  |
| StartupSelector   | PathSelector  | Pasta do projeto de startup            | Caminho de pasta                                                  |
| ActionCombo       | ComboBox      | “Add Migration” / “Update Database”    | Tag = Add/Update; define comando `dotnet ef`                      |
| ProviderCombo     | ComboBox      | “SQL Server” / “SQLite”                | Tag = SqlServer/Sqlite; define provider                           |
| MigrationNameInput| TextBox       | Nome da migration                      | Identificador da migration                                        |
| DbContextInput    | TextBox       | Full name do DbContext                 | String usada como `--context`                                     |
| DryRunCheck       | CheckBox      | “Dry Run (Gerar comando sem executar)” | Booleano; gera comando sem executar                               |
| OutputText        | TextBox (RO)  | Log de saída                           | Texto do comando `dotnet ef`                                      |

---

## NgrokWindow – Expor Porta (Ngrok)

Arquivo: `Views/NgrokWindow.xaml`

| ID               | Tipo            | Exibe                                   | Espera / representa                                               |
| ---------------- | --------------- | --------------------------------------- | ----------------------------------------------------------------- |
| ProfileSelector  | ProfileSelector | Nome do perfil Ngrok                    | Porta/configs salvas para o perfil                                |
| PortInput        | TextBox         | Porta local (ex.: 5000)                 | Porta numérica local                                              |
| StartButton      | Button          | “Expor Porta”                           | Dispara criação do túnel Ngrok                                    |
| TunnelsList      | ListBox         | Lista de túneis ativos                  | Items com PublicUrl e Config.Addr via binding                     |
| EmptyStateText   | TextBlock       | “Nenhum túnel ativo.”                   | Mensagem quando a lista está vazia                                |

---

## SearchTextWindow – Buscar Texto

Arquivo: `Views/SearchTextWindow.xaml`

| ID                   | Tipo          | Exibe                              | Espera / representa                                      |
| -------------------- | ------------- | ---------------------------------- | -------------------------------------------------------- |
| ProfileSelector      | ProfileSelector | Perfil de busca                   | Combinação de path, regex, include/exclude etc.         |
| PathSelector         | PathSelector  | Diretório de busca                 | Pasta raiz para varrer arquivos                         |
| SearchTextInput      | TextBox       | Texto ou regex                     | String usada como termo de busca                        |
| UseRegexCheck        | CheckBox      | “Usar Regex”                       | Booleano; alterna entre match literal e regex           |
| CaseSensitiveCheck   | CheckBox      | “Case Sensitive”                   | Booleano                                                 |
| IncludePatternInput  | TextBox       | Globs de inclusão                  | Lista de padrões (ex.: *.cs, *.ts)                      |
| ExcludePatternInput  | TextBox       | Globs de exclusão                  | Lista de padrões (ex.: bin, obj, node_modules)          |
| OutputText           | TextBox (RO)  | Resultados da busca                | Texto formatado com os matches                          |

---

## OrganizerWindow – Organizer

Arquivo: `Views/OrganizerWindow.xaml`

| ID                 | Tipo            | Exibe                                   | Espera / representa                                               |
| ------------------ | --------------- | --------------------------------------- | ----------------------------------------------------------------- |
| ProfileSelector    | ProfileSelector | Perfil de organização                   | Mapeamento de categorias/regras                                   |
| InputPathSelector  | PathSelector    | Pasta de entrada                        | Diretório de origem                                               |
| OutputPathSelector | PathSelector    | Pasta de saída (opcional)               | Diretório de destino                                              |
| SimulateCheck      | CheckBox        | “Simular (Apenas Teste)”                | Booleano; se true, não move arquivos de fato                      |

---

## HarvestWindow – Harvest

Arquivo: `Views/HarvestWindow.xaml`

| ID                    | Tipo            | Exibe                           | Espera / representa                               |
| --------------------- | --------------- | --------------------------------| ------------------------------------------------- |
| ProfileSelector       | ProfileSelector | Perfil de harvest               | Configurações completas de análise                |
| SourcePathSelector    | PathSelector    | Diretório de origem             | Pasta a ser analisada                             |
| OutputPathSelector    | PathSelector    | Diretório de destino            | Pasta de backup                                   |
| ConfigPathSelector    | PathSelector    | Arquivo de configuração         | Caminho de arquivo de config                      |
| MinScoreBox           | TextBox         | Pontuação mínima                | Número (double/int)                               |
| CopyFilesCheck        | CheckBox        | “Copiar Arquivos”               | Booleano                                          |
| RememberSettingsCheck | CheckBox        | “Lembrar configurações”         | Booleano                                          |

---

## RenameWindow – Renomear em Massa

Arquivo: `Views/RenameWindow.xaml`

| ID                    | Tipo            | Exibe                           | Espera / representa                               |
| --------------------- | --------------- | --------------------------------| ------------------------------------------------- |
| ProfileSelector       | ProfileSelector | Perfil de rename                | Configurações salvas de rename                    |
| RootPathSelector      | PathSelector    | Pasta raiz                      | Diretório onde o rename roda                      |
| OldTextBox            | TextBox         | Texto antigo                    | String alvo                                       |
| NewTextBox            | TextBox         | Texto novo                      | String de substituição                            |
| ModeCombo             | ComboBox        | Modo de rename                  | Algoritmo (geral, namespace inteligente)          |
| IncludeBox            | TextBox         | Globs de inclusão               | Lista de padrões                                  |
| ExcludeBox            | TextBox         | Globs de exclusão               | Lista de padrões                                  |
| BackupCheck           | CheckBox        | “Criar Backup”                  | Booleano                                          |
| UndoLogCheck          | CheckBox        | “Log de Reversão”               | Booleano                                          |
| DryRunCheck           | CheckBox        | “Dry Run (Teste)”               | Booleano; simula sem alterar arquivos             |
| RememberSettingsCheck | CheckBox        | “Lembrar”                       | Booleano; persiste última configuração            |

---

## SnapshotWindow – Snapshot

Arquivo: `Views/SnapshotWindow.xaml`

| ID                 | Tipo            | Exibe                           | Espera / representa                               |
| ------------------ | --------------- | --------------------------------| ------------------------------------------------- |
| ProfileSelector    | ProfileSelector | Perfil de snapshot              | Perfil com paths e formatos                       |
| RootPathSelector   | PathSelector    | Pasta raiz do projeto           | Diretório base do snapshot                        |
| TextCheck          | CheckBox        | “Texto Unificado (.txt)”        | Booleano                                          |
| HtmlCheck          | CheckBox        | “Preview HTML (.html)”          | Booleano                                          |
| JsonNestedCheck    | CheckBox        | “JSON Aninhado (.json)”         | Booleano                                          |
| JsonRecursiveCheck | CheckBox        | “JSON Recursivo (.json)”        | Booleano                                          |

