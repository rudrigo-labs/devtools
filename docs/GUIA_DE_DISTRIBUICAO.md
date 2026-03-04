# Guia de Distribuicao - DevTools

Este guia descreve o fluxo oficial para gerar binarios e instalador do DevTools.

## Escopo

- Interface oficial: WPF (`DevTools.Presentation.Wpf`)
- CLI: obsoleto (nao entra no build de distribuicao)

## Pre-requisitos

- Windows 10/11
- .NET SDK 10.0+
- Inno Setup 6 (`ISCC.exe`)

## Fluxo oficial (recomendado)

Use o script da raiz `build/build_installer.bat`:

```powershell
build\build_installer.bat 1.0.0
```

O script faz:

1. `dotnet publish` da WPF em `Release` para `win-x64`
2. geracao do instalador com `build/DEVTOOLS_SETUP_BUILD.iss`
3. inclusao do manual oficial (`MANUAL.md`) no instalador

Saida:

- `build/out/wpf`
- `build/out/installer/DevTools_Setup.exe`

## Conteudo instalado

Pelo script `.iss`, o instalador copia:

- executavel e dependencias em `{app}\bin`
- manual em `{app}\docs\MANUAL_DEVTOOLS.md`

Atalhos criados:

- Menu iniciar: `DevTools`
- Area de trabalho: `DevTools`
- Menu iniciar: `Manual do DevTools`

## Validacao recomendada antes de publicar

1. Executar build e testes:

```powershell
dotnet build src/DevTools.slnx -c Release
dotnet test src/Tools/DevTools.Tests/DevTools.Tests.csproj -c Release
```

2. Gerar instalador com versao.
3. Instalar em maquina limpa (ou VM).
4. Validar:
- abertura da WPF
- abrir/fechar ferramentas principais
- comportamento de bandeja
- leitura do manual instalado

## Observacoes

- Configuracoes de usuario ficam em `%AppData%\DevTools`.
- Atualizacao de versao nao deve apagar dados de usuario.
- O projeto CLI nao faz parte do instalador.
