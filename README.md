# DevTools

Suite de produtividade para desenvolvedores, focada em Windows, com shell WPF em estilo IDE, execucao via tray e ferramentas tecnicas para rotina de engenharia.

## Estado atual

- Interface oficial: WPF (`src/Presentation/DevTools.Presentation.Wpf`)
- Projeto CLI: obsoleto (mantido no repositorio apenas para referencia historica)
- Persistencia: JSON (padrao) ou SQLite (modo opcional)

## Ferramentas disponiveis

- Notes: notas locais (`.txt`/`.md`), backup ZIP e sincronizacao opcional com Google Drive
- Organizer: organizacao de arquivos por regras/categorias
- Harvest: coleta de arquivos para analise
- SearchText: busca textual em massa com filtros
- Rename: renomeacao/refatoracao em lote
- Snapshot: snapshot estrutural de projeto
- Utf8Convert: conversao de encoding para UTF-8
- Image Splitter: fatiamento de imagens
- Migrations: apoio a comandos de migracao
- SSH Tunnel: tunel SSH com perfis
- Ngrok: gerenciamento de tunel ngrok
- Jobs/Logs: acompanhamento de execucoes e diagnostico

## Arquitetura

- `src/Core`: contratos, modelos e utilitarios base
- `src/Tools`: engines das ferramentas
- `src/Presentation/DevTools.Presentation.Wpf`: shell, UI, temas, tray e configuracoes
- `docs`: documentacao tecnica, operacional e planos

## Requisitos

- Windows 10/11
- .NET SDK 10.0+
- Inno Setup 6 (apenas para gerar instalador)

## Executar em desenvolvimento

```powershell
dotnet build src/DevTools.slnx -c Debug
dotnet run --project src/Presentation/DevTools.Presentation.Wpf/DevTools.Presentation.Wpf.csproj
```

## Testes

```powershell
dotnet test src/Tools/DevTools.Tests/DevTools.Tests.csproj -c Debug
```

## Gerar instalador

Script oficial:

```powershell
build\build_installer.bat 1.0.0
```

Saida:

- `build/out/wpf`
- `build/out/installer/DevTools_Setup.exe`

O instalador inclui o manual oficial em:

- `{app}\docs\MANUAL_DEVTOOLS.md`

## Documentacao oficial

- Manual de uso completo: [MANUAL.md](MANUAL.md)
- Cobertura de testes: [docs/INTEGRATION_TEST_COVERAGE.md](docs/INTEGRATION_TEST_COVERAGE.md)
- Relatorio de varredura: [docs/RELATORIO_TESTES_E_VARREDURA_GERAL_2026-03-04.md](docs/RELATORIO_TESTES_E_VARREDURA_GERAL_2026-03-04.md)

## Licenca

MIT. Consulte [LICENSE](LICENSE).
