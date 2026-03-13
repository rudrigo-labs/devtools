# DevTools

> Status: Em desenvolvimento ativo.
> Este projeto ainda esta em evolucao e pode sofrer mudancas de arquitetura, UX e contratos internos.

Suite desktop em WPF para executar ferramentas de produtividade de desenvolvimento com arquitetura em camadas:
- Host (WPF)
- Core (contratos e modelos compartilhados)
- Tools (regra de negocio por ferramenta)
- Infrastructure (persistencia SQLite com EF Core)

Este README e a referencia de entrada do projeto: setup, arquitetura, navegacao, ferramentas e operacao.

## 1. Visao geral

O DevTools centraliza ferramentas de automacao em uma unica aplicacao desktop.

Principios do projeto:
1. A UI nunca executa regra de negocio diretamente.
2. Toda execucao passa por `Facade -> Engine`.
3. Persistencia funcional em SQLite.
4. Cada ferramenta vive em projeto proprio na camada `Tools`.

## 2. Arquitetura

### 2.1 Camadas

1. `src/Host/DevTools.Host.Wpf`
- Shell WPF, navegacao, telas e facades.

2. `src/Core/DevTools.Core`
- Contratos comuns (`RunResult`, validacao, abstractions etc).

3. `src/Tools/DevTools.<Tool>`
- Models, validators, services, engines e contratos de repositorio por ferramenta.

4. `src/Infrastructure/DevTools.Infrastructure`
- Repositorios concretos, `DbContext`, migrations e bootstrap de SQLite.

### 2.2 Fluxo de execucao canonico

1. Usuario interage na View.
2. View monta request e chama Facade.
3. Facade delega para Engine.
4. Engine valida request.
5. Engine executa regra de negocio.
6. Engine chama repositorio (interface da Tool).
7. Infrastructure persiste/consulta no SQLite.
8. Resultado retorna como `RunResult<T>`.

## 3. Stack tecnologica

- .NET 10 (`net10.0`)
- WPF (`net10.0-windows`)
- EF Core 9 + SQLite
- DI com `Microsoft.Extensions.DependencyInjection`
- Material Design Themes (WPF)
- Integracao Google Drive (Notes)

## 4. Requisitos

1. Windows (Host e WPF).
2. .NET SDK 10 instalado.
3. Acesso local de escrita ao `%APPDATA%` (ou definir `DEVTOOLS_SQLITE_PATH`).

## 5. Setup rapido

### 5.1 Restore e build

```powershell
dotnet restore src/DevTools.slnx
dotnet build src/DevTools.slnx -v minimal
```

### 5.2 Executar aplicacao

```powershell
dotnet run --project src/Host/DevTools.Host.Wpf/DevTools.Host.Wpf.csproj
```

## 6. Estrutura do repositorio

```text
devtools/
|-- docs/
|-- src/
|   |-- Core/
|   |-- Host/
|   |-- Infrastructure/
|   |-- Tools/
|   |-- DevTools.slnx
|   `-- excluir-bin-obj.ps1
`-- README.md
```

## 7. Ferramentas disponiveis

1. Snapshot
- Gera snapshot textual do projeto (texto/html/json).

2. Rename
- Renomeacao em lote de texto/identificadores.

3. Harvest
- Mineracao de codigo reutilizavel.

4. Image Split
- Recorte automatico de componentes de imagem.

5. Search Text
- Busca textual com suporte a regex e globs.

6. Organizer
- Organizacao/classificacao de arquivos.

7. UTF-8 Convert
- Conversao de arquivos para UTF-8.

8. Migrations
- Execucao de `dotnet ef` para migrations.

9. SSH Tunnel
- Abertura/encerramento de tunel SSH.

10. Ngrok
- Exposicao de porta local por tunel publico.

11. Notes
- Editor de notas com persistencia local e opcao de Google Drive.

## 8. Navegacao e UX atual

Fluxo padrao atual:
1. App inicia em `Home`.
2. Home funciona como launcher de ferramentas.
3. Sidebar continua disponivel para navegacao direta.
4. Ferramentas com fluxo configuracao-primeiro abrem em modo de configuracao.

Fluxo configuracao-primeiro:
- Snapshot
- Migrations
- Utf8Convert
- Notes

Documentacao desta parte:
- `docs/devtools-navegacao-ux.md`

## 9. Persistencia (SQLite)

### 9.1 Local padrao do banco

Por padrao:
- `%APPDATA%/DevTools/devtools.db`

Override por variavel de ambiente:
- `DEVTOOLS_SQLITE_PATH`

### 9.2 Bootstrap

No startup, o Host executa `SqliteBootstrapper.Migrate()`.
Se houver banco legado, o bootstrap registra baseline e segue migrations.

## 10. Configuracao da aplicacao

Arquivo:
- `src/Host/DevTools.Host.Wpf/appsettings.json`

Exemplo atual:

```json
{
  "DevTools": {
    "FileTools": {
      "MaxFileSizeKb": 500,
      "AbsoluteMaxFileSizeKb": 10000
    }
  }
}
```

## 11. Notes + Google Drive

A ferramenta Notes suporta:
1. armazenamento local de notas (`.md`/`.txt`)
2. import/export zip
3. integracao opcional com Google Drive

Campos principais de integracao:
- `credentials.json`
- folder id do Drive
- pasta de token OAuth

Guia dedicado:
- `docs/notes-telas.md`

## 12. Documentacao adicional

1. Guia geral das telas:
- `docs/telas-ferramentas-guia.md`

2. Documento detalhado de ferramentas:
- `docs/documentacao-ferramentas-devtools.md`

3. Navegacao e UX:
- `docs/devtools-navegacao-ux.md`

## 13. Comandos uteis

1. Build da solucao:

```powershell
dotnet build src/DevTools.slnx -v minimal
```

2. Build apenas Host:

```powershell
dotnet build src/Host/DevTools.Host.Wpf/DevTools.Host.Wpf.csproj -v minimal
```

3. Limpar `bin/obj`:

```powershell
powershell -ExecutionPolicy Bypass -File src/excluir-bin-obj.ps1
```

## 14. Como adicionar nova ferramenta (resumo)

1. Criar projeto em `src/Tools/DevTools.<Nome>`.
2. Implementar models, validators, service e engine.
3. Criar interface de repositorio na Tool.
4. Implementar repositorio concreto na Infrastructure.
5. Criar Facade no Host.
6. Criar `WorkspaceView` no Host.
7. Registrar DI em `App.xaml.cs`.
8. Registrar navegacao em `MainWindow`.
9. Criar/aplicar migration quando fechar integracao fim-a-fim.
10. Validar build e fluxo manual (salvar/rodar/cancelar).

## 15. Troubleshooting rapido

1. Erros estranhos de build apos muitas mudancas:
- limpar `bin/obj` e build novamente.

2. Problemas de banco local:
- validar permissao de escrita no `%APPDATA%`
- validar `DEVTOOLS_SQLITE_PATH` se estiver definido

3. Tela sem atualizar ao trocar tool:
- conferir registro no `_toolRegistry` da `MainWindow`
- conferir registro no DI (`App.xaml.cs`)

## 16. Boas praticas de contribuicao

1. Nao acoplar UI com regra de negocio.
2. Nao acessar `DbContext` direto da camada Host/Tool.
3. Manter validacao no dominio (validator/service/engine).
4. Preservar padrao visual das telas (`ToolHeader -> ActionBar -> ToolBody -> Status`).
5. Garantir build limpo antes de abrir PR/merge.
