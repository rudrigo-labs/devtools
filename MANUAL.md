# Manual Oficial do DevTools

Este manual descreve o uso completo do DevTools na versao atual baseada em WPF.

## 1. Objetivo do sistema

O DevTools e uma suite para acelerar tarefas tecnicas do dia a dia:

- processamento de arquivos e texto
- suporte a manutencao de codigo
- operacoes de infra (tunel SSH, ngrok, migrations)
- gestao de notas locais com backup

## 2. Como iniciar

### 2.1 Execucao em desenvolvimento

```powershell
dotnet run --project src/Presentation/DevTools.Presentation.Wpf/DevTools.Presentation.Wpf.csproj
```

### 2.2 Comportamento na bandeja (tray)

- Duplo clique no icone: abre o shell principal
- Menu de contexto: abre ferramentas diretamente
- Fechar o shell: oferece minimizar para bandeja ou encerrar

## 3. Shell principal (IDE-style)

O shell possui tres areas principais:

1. Ferramentas
2. Execucoes (Jobs)
3. Configuracoes

### 3.1 Ferramentas

Abre as janelas de cada modulo funcional.

### 3.2 Jobs

Mostra tarefas em execucao/concluidas, com status e logs.

### 3.3 Configuracoes

Centraliza configuracoes globais e perfis por ferramenta.

## 4. Ferramentas e uso

## 4.1 Notes

Funcao:

- criar, editar e listar notas
- salvar em `.txt` ou `.md`
- exportar/importar backup ZIP
- sincronizacao opcional com Google Drive

Fluxo de salvamento:

1. salva localmente
2. se sincronizacao estiver habilitada, envia para nuvem

## 4.2 Organizer

Funcao:

- organizar arquivos em categorias
- aplicar regras por extensao/palavras-chave

Uso basico:

1. definir pasta de entrada
2. definir pasta de saida
3. executar

## 4.3 Harvest

Funcao:

- coletar arquivos de projeto para pasta consolidada

Uso basico:

1. selecionar origem
2. selecionar destino
3. ajustar score/filtros
4. executar

## 4.4 SearchText

Funcao:

- localizar texto em massa em arquivos

Uso basico:

1. informar raiz
2. informar termo ou padrao
3. executar busca

## 4.5 Rename

Funcao:

- renomeacao/refatoracao em lote

Uso basico:

1. definir raiz
2. informar texto antigo/novo
3. configurar filtros e dry-run
4. executar

## 4.6 Snapshot

Funcao:

- gerar snapshot estrutural de projeto

Uso basico:

1. selecionar raiz
2. selecionar formatos
3. executar

## 4.7 Utf8Convert

Funcao:

- converter arquivos para UTF-8 em lote

Uso basico:

1. selecionar raiz
2. executar conversao

## 4.8 Image Splitter

Funcao:

- dividir imagem em partes menores

Uso basico:

1. selecionar imagem de entrada
2. selecionar pasta de saida
3. ajustar parametros
4. executar

## 4.9 Migrations

Funcao:

- executar comandos de migration com apoio visual

Uso basico:

1. selecionar projeto
2. selecionar startup project
3. informar DbContext
4. escolher acao
5. executar

## 4.10 SSH Tunnel

Funcao:

- abrir tunel SSH com perfil

Uso basico:

1. preencher host/usuario/chave/portas
2. iniciar tunel
3. encerrar quando finalizar uso

## 4.11 Ngrok

Funcao:

- iniciar e acompanhar tunel ngrok

Uso basico:

1. configurar executavel/token
2. iniciar tunel
3. acompanhar URL publica

## 4.12 Logs

Funcao:

- visualizar e limpar logs de aplicacao

## 5. Configuracoes

## 5.1 Perfis por ferramenta

Permite salvar configuracoes recorrentes por ferramenta.

## 5.2 Configuracoes de Notes e Nuvem

- pasta local das notas
- formato padrao (`.txt`/`.md`)
- auto sync
- credenciais Google Drive via UI
- teste de conexao

## 5.3 Outras configuracoes

- Harvest
- Organizer
- Migrations
- Ngrok

## 6. Persistencia

Modo padrao:

- JSON para configuracoes

Modo opcional:

- SQLite, habilitado por variavel de ambiente:

```powershell
$env:DEVTOOLS_STORAGE_BACKEND="sqlite"
```

## 7. Build, testes e release

## 7.1 Build local

```powershell
dotnet build src/DevTools.slnx -c Debug
```

## 7.2 Testes

```powershell
dotnet test src/Tools/DevTools.Tests/DevTools.Tests.csproj -c Debug
```

## 7.3 Gerar instalador

```powershell
build\build_installer.bat 1.0.0
```

Saida esperada:

- `build/out/installer/DevTools_Setup.exe`

## 8. Troubleshooting rapido

## 8.1 Janela travando ao testar conexao

- validar se campos de credencial estao preenchidos
- revisar mensagem de validacao no painel de Notes e Nuvem

## 8.2 Erro de inicializacao WPF por recurso de tema

- validar dicionarios de tema carregados
- rodar build limpo e reabrir

## 8.3 Problemas de permissao de pasta

- testar execucao com pasta de trabalho local do usuario
- evitar caminhos protegidos sem permissao

## 9. Escopo atual

- Interface oficial: WPF
- Projeto CLI: obsoleto e fora da solution/instalador

Fim do manual.
