# Configuracoes e Perfis - Guia Detalhado

Data de referencia: 2026-03-05

Este documento e a referencia completa de configuracoes, persistencia e perfis do DevTools.

## 1. Visao geral

O DevTools separa dados em 3 grupos:

1. preferencias de aplicacao (`settings.json`)
2. configuracoes globais por secao (`ConfigService`)
3. perfis por ferramenta (`ProfileManager`)

## 2. Persistencia: onde cada coisa fica

## 2.1 Preferencias da aplicacao

- Arquivo: `%AppData%\DevTools\settings.json`
- Servico: `SettingsService`
- Exemplos:
  - posicao de janelas
  - ultimos caminhos usados
  - preferencias leves do shell

## 2.2 Configuracoes globais

- Servico: `ConfigService`
- API: `GetSection<T>(nome)` / `SaveSection<T>(nome, dados)`

Backend JSON (padrao):

- arquivo `appsettings.json` no mesmo diretorio do executavel WPF

Backend SQLite (opcional):

- banco `%AppData%\DevTools\devtools.db`
- tabela `AppSettings` (chave/valor serializado)

Ativacao de backend:

- `DEVTOOLS_STORAGE_BACKEND=json` (padrao)
- `DEVTOOLS_STORAGE_BACKEND=sqlite`
- ou pelo painel: `Configuracoes > Armazenamento`

## 2.3 Perfis por ferramenta

- Servico: `ProfileManager` + `ProfileUIService`

Backend JSON:

- pasta `%AppData%\DevTools\profiles\`
- arquivo por ferramenta: `devtools.<ferramenta>.json`

Backend SQLite:

- tabela `ToolProfiles` em `%AppData%\DevTools\devtools.db`

Estrutura do perfil:

- `Name`
- `IsDefault`
- `UpdatedUtc`
- `Options` (dicionario `chave -> valor`)

Regra de negocio:

- so um perfil default por ferramenta

## 2.4 Configuracoes Ngrok (store proprio)

A Tool Ngrok usa store proprio, independente de `ConfigService`.

Backend JSON:

- `%AppData%\DevTools\ngrok.settings.json`

Backend SQLite:

- tabela `NgrokSettings` no banco `%AppData%\DevTools\devtools.db`

Campos:

- `ExecutablePath`
- `AuthToken`
- `AdditionalArgs`

## 2.5 Notas

- Notas sempre ficam em arquivo fisico local (`.txt`/`.md`)
- Caminho padrao: `%UserProfile%\Documents\DevTools\Notes`
- Mesmo com backend SQLite ativo, notas continuam em arquivo

## 3. Painel de Configuracoes (MainWindow)

## 3.1 Armazenamento

Campo:

- modo `JSON` ou `SQLite`

Comportamento:

- salva variavel de ambiente de usuario/processo
- pede reinicio para aplicar troca

## 3.2 Notes e Nuvem

### Notas

- pasta de armazenamento
- formato padrao (`.txt`, `.md`)
- exibicao inicial da lista (`Auto`, `8`, `15`, `20`)
- auto sync (bool)

### Google Drive

- habilitar backup
- `ClientId`
- `ClientSecret`
- `ProjectId`
- `FolderName`

Validacao:

- se backup habilitado, todos os campos acima sao obrigatorios
- botao `Testar Conexao` valida campos antes de executar teste

## 3.3 Harvest

Campos principais:

- extensoes permitidas
- pastas ignoradas
- tamanho maximo de arquivo
- score minimo e top default
- pesos de relevancia

Default importante (pastas ignoradas):

- `bin,obj,.git,.vs,node_modules,dist,build,.idea,.vscode,.next,.nuxt,.turbo,Snapshot`

## 3.4 Organizer

- lista de categorias
- formulario de categoria com:
  - nome
  - pasta destino
  - palavras-chave positivas/negativas
  - pesos e min score

## 3.5 Migrations

Campos:

- `RootPath`
- `StartupProjectPath`
- `DbContextFullName`
- `AdditionalArgs`

Validacao atual:

- todos esses campos sao obrigatorios na tela

## 3.6 Ngrok

Campos:

- caminho do executavel (opcional se ja estiver no PATH)
- auth token (obrigatorio para salvar)
- args adicionais

Deteccao automatica implementada:

- localiza executavel (caminho configurado ou PATH)
- localiza `ngrok.yml` em `%USERPROFILE%\.ngrok2\` ou `%USERPROFILE%\AppData\Local\ngrok\`
- extrai `authtoken:` quando existir no YAML

## 4. Perfis: chaves por ferramenta

## 4.1 Rename

Chaves:

- `old-text`
- `new-text`
- `include`
- `exclude`

## 4.2 Migrations

Chaves:

- `root-path`
- `startup-path`
- `dbcontext`

## 4.3 Harvest

Chaves:

- `source-path`
- `output-path`
- `min-score`

## 4.4 SearchText

Chaves:

- `root-path`
- `search-pattern`
- `include`
- `exclude`

## 4.5 Snapshot

Chave:

- `project-path`

## 4.6 SSHTunnel

Chaves principais:

- `ssh-profile-name`
- `ssh-host`
- `ssh-port`
- `ssh-user`
- `identity-file`
- `local-bind`
- `local-port`
- `remote-host`
- `remote-port`

## 5. Validacoes e mensagens

Padrao visual:

- validacoes de formulario: mensagem inline no painel
- dialogos de confirmacao/informacao: `UiMessageService`

Padrao de obrigatoriedade:

- `Os campos abaixo nao podem ficar em branco:`

## 6. Recomendacoes operacionais

1. padronizar backend (JSON/SQLite) por ambiente antes de uso intensivo
2. manter backup de `%AppData%\DevTools` em troca de maquina
3. validar Google Drive e Ngrok com teste real apos cadastrar credenciais
4. revisar defaults com base no uso real do time (voce ja sinalizou que vai passar defaults finais)
