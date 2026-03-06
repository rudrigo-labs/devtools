# Definicao Fase 1 - Modelo Unificado de Configuracoes por Ferramenta

- Data/Hora: 2026-03-06 13:42
- Demanda: `DEFINICAO_REFATORACAO_ENTIDADES_FERRAMENTAS_20260306_1315.md`

## Objetivo da Fase 1
Definir modelo padrao para configuracoes nomeadas por ferramenta, com nomenclatura unica,
campos comuns e matriz de obrigatoriedade, removendo ambiguidade de "configuracao/projeto".

## Nomenclatura final
- Termo canonico para item salvo de configuracao: `ConfiguracaoNomeada`.
- UI pode apresentar rotulo de dominio por ferramenta, mas semanticamente e sempre uma configuracao nomeada.

Rotulos de UI recomendados por ferramenta:
- Migrations: "Projeto"
- Snapshot: "Projeto"
- SSH Tunnel: "Conexao"
- Ngrok: "Conexao"
- Harvest/Organizer/SearchText/Rename/Image/Utf8: "Configuracao"
- Notes: manter fluxo proprio (nao baseado em configuracoes)

## Estrutura base comum (conceitual)
`NamedToolConfiguration`
- `Id` (identificador tecnico, PK)
- `ToolSlug` (identifica a ferramenta)
- `Name` (nome funcional exibido)
- `Description` (descricao curta)
- `IsActive` (ativo/inativo)
- `IsDefault` (padrao para a ferramenta)
- `CreatedAtUtc`
- `UpdatedAtUtc`
- `Payload` (dados especificos da ferramenta)

## Regras base do modelo
1. `ToolSlug + Name` deve ser unico por ferramenta.
2. `Name` obrigatorio em todas as ferramentas com configuracao nomeada.
3. `Payload` obrigatorio quando a ferramenta depende de parametros para executar.
4. `IsDefault` e opcional, mas se existir mais de um item ativo pode haver no maximo 1 default por ferramenta.
5. Itens inativos nao devem ser carregados automaticamente no fluxo de execucao.

## Matriz de obrigatoriedade por ferramenta (fase 1)

### Migrations
- Nome da configuracao: obrigatorio
- RootPath: obrigatorio
- StartupProjectPath: obrigatorio
- DbContextFullName: obrigatorio
- Target provider path(s): obrigatorio conforme provider
- AdditionalArgs: opcional

### Snapshot
- Nome da configuracao: obrigatorio
- RootPath: obrigatorio
- OutputBasePath: opcional
- Flags de output (>=1): obrigatorio
- IgnoredDirectories: opcional
- MaxFileSizeKb: opcional

### SSH Tunnel
- Nome da configuracao: obrigatorio
- SshHost/SshPort/SshUser: obrigatorio
- IdentityFile: obrigatorio
- LocalBindHost/LocalPort: obrigatorio
- RemoteHost/RemotePort: obrigatorio
- StrictHostKeyChecking: opcional (default definido)
- ConnectTimeoutSeconds: opcional (default definido)

### Ngrok
- Nome da configuracao: obrigatorio
- AuthToken: obrigatorio
- Port: obrigatorio para StartHttp
- ExecutablePath: opcional (quando em PATH)
- AdditionalArgs: opcional

### Harvest
- Nome da configuracao: obrigatorio
- RootPath: obrigatorio
- OutputPath: obrigatorio
- MinScore/CopyFiles: opcionais
- Rules/Weights/Categories: obrigatorios no conjunto de configuracao (podem vir por default editavel)

### Organizer
- Nome da configuracao: obrigatorio
- InboxPath: obrigatorio
- OutputPath: opcional
- MinScore/Apply: opcionais
- AllowedExtensions/Categories: obrigatorios no conjunto de configuracao

### SearchText
- Nome da configuracao: obrigatorio
- RootPath: obrigatorio
- Pattern: obrigatorio
- Include/Exclude/flags: opcionais

### Rename
- Nome da configuracao: obrigatorio
- RootPath: obrigatorio
- OldText/NewText: obrigatorios
- Mode: obrigatorio
- Demais opcoes: opcionais

### Notes
- Mantem fluxo de dominio proprio (nao migra para ConfiguracaoNomeada nesta fase)
- StoragePath/default format/sync: obrigatorios conforme configuracao global de notas

### Image Splitter
- Nome da configuracao: obrigatorio
- InputPath: obrigatorio
- OutputDirectory: opcional
- Parametros de split: opcionais com defaults validos

### UTF8 Convert
- Nome da configuracao: obrigatorio
- RootPath: obrigatorio
- Recursive: obrigatorio (bool)
- Demais opcoes: opcionais

## Decisoes da fase
1. Eliminar ambiguidade de termos (configuracao/projeto) no nivel de modelo.
2. Preservar rotulo de dominio na UI por ferramenta sem duplicar conceito.
3. Tratar obrigatoriedade por matriz e nao por inferencia solta de tela.

## Saida da fase
- Modelo conceitual definido.
- Matriz de obrigatoriedade definida.
- Nomenclatura final definida para seguir na Fase 2.

