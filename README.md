# DevTools

Suite de produtividade para desenvolvedores no Windows, com shell WPF estilo IDE, roteamento de ferramentas por abas/janelas e suporte a execucao via system tray.

## Status da versao

- Interface oficial: WPF (`src/Presentation/DevTools.Presentation.Wpf`)
- CLI: obsoleto (fora da solution e fora do instalador)
- Persistencia de configuracoes: JSON (padrao) ou SQLite (opcional via UI)
- Validacao tecnica em `2026-03-04`:
  - `dotnet build src/DevTools.slnx -c Debug` -> sucesso
  - `dotnet test src/Tools/DevTools.Tests/DevTools.Tests.csproj -c Debug --no-build` -> 35 aprovados, 2 ignorados

## Ferramentas disponiveis

- Notes: notas locais (`.txt`/`.md`), backup ZIP e sync opcional com Google Drive
- Organizer: organizacao de arquivos por regras/categorias
- Harvest: coleta de arquivos para analise
- SearchText: busca textual em lote com filtros
- Rename: renomeacao/refatoracao em lote
- Snapshot: snapshot estrutural de projeto
- Utf8Convert: conversao de encoding para UTF-8
- Image Splitter: fatiamento de imagens
- Migrations: apoio a comandos de migracao
- SSH Tunnel: tunel SSH com perfil
- Ngrok: gerenciamento de tunel ngrok
- Jobs/Logs: monitoramento de execucoes e diagnostico

## Estrutura

- `src/Core`: contratos, modelos e utilitarios base
- `src/Tools`: engines de cada ferramenta
- `src/Presentation/DevTools.Presentation.Wpf`: shell, UI, temas, tray, configuracoes
- `docs`: guias operacionais, tecnicos e relatorios

## Requisitos

- Windows 10/11
- .NET SDK 10.0+
- Inno Setup 6 (somente para gerar instalador)

## Executar localmente

```powershell
dotnet build src/DevTools.slnx -c Debug
dotnet run --project src/Presentation/DevTools.Presentation.Wpf/DevTools.Presentation.Wpf.csproj
```

## Testes

```powershell
dotnet test src/Tools/DevTools.Tests/DevTools.Tests.csproj -c Debug
```

## Build de instalador

```powershell
build\build_installer.bat 1.0.0
```

Saidas:

- `build/out/wpf`
- `build/out/installer/DevTools_Setup.exe`

Manual incluido no instalador:

- `{app}\docs\MANUAL_DEVTOOLS.md`

## Documentacao oficial

- Manual do sistema: [MANUAL.md](MANUAL.md)
- Mapa de docs: [docs/README.md](docs/README.md)
- Cobertura de testes: [docs/INTEGRATION_TEST_COVERAGE.md](docs/INTEGRATION_TEST_COVERAGE.md)
- Relatorio de fechamento: [docs/RELATORIO_FECHAMENTO_VERSAO_2026-03-04.md](docs/RELATORIO_FECHAMENTO_VERSAO_2026-03-04.md)

## Melhorias futuras (proxima versao)

1. Consolidar infraestrutura de testes WPF para remover os 2 testes atualmente ignorados por afinidade de thread em `Application.Current`.
2. Promover SQLite a backend padrao com assistente guiado de migracao JSON -> SQLite.
3. Exportar/importar pacote unico de configuracoes e perfis.
4. Criar painel de saude operacional no shell (jobs, SSH, ngrok, backend, storage path).
5. Automatizar changelog e notas de release por tag.

## Licenca

MIT. Consulte [LICENSE](LICENSE).
