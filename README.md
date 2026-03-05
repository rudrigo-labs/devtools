# DevTools

Suite desktop para produtividade tecnica no dia a dia de desenvolvimento.

## Estado atual

- Interface oficial: WPF
- CLI: obsoleto e fora da entrega oficial
- Persistencia: JSON (padrao) ou SQLite (opcional)
- Testes: 36 aprovados, 2 ignorados, 0 falhas

## Ferramentas

- Notes
- Organizer
- Harvest
- SearchText
- Rename
- Snapshot
- Utf8Convert
- Image Splitter
- Migrations
- SSH Tunnel
- Ngrok
- Jobs / Logs

## Executar

```powershell
dotnet build src/DevTools.slnx -c Debug
dotnet run --project src/Presentation/DevTools.Presentation.Wpf/DevTools.Presentation.Wpf.csproj
```

## Testar

```powershell
dotnet test src/Tools/DevTools.Tests/DevTools.Tests.csproj -v minimal
```

## Instalador

```powershell
build\build_installer.bat 1.0.0
```

## Documentacao oficial

- Manual detalhado: [MANUAL.md](MANUAL.md)
- Configuracoes e perfis: [docs/CONFIGURACOES_E_PERFIS_DETALHADO.md](docs/CONFIGURACOES_E_PERFIS_DETALHADO.md)
- Documento tecnico: [docs/TechnicalDoc.md](docs/TechnicalDoc.md)
- Cobertura de testes: [docs/INTEGRATION_TEST_COVERAGE.md](docs/INTEGRATION_TEST_COVERAGE.md)
- Testes manuais pendentes: [docs/TESTES_MANUAIS_PENDENTES_2026-03-05.md](docs/TESTES_MANUAIS_PENDENTES_2026-03-05.md)
- Pendencias da proxima versao: [docs/PENDENCIAS_UI_E_HUB.md](docs/PENDENCIAS_UI_E_HUB.md)
- Apresentacao nao tecnica (curriculo/LinkedIn): [docs/APRESENTACAO_DEVTOOLS_LINKEDIN.md](docs/APRESENTACAO_DEVTOOLS_LINKEDIN.md)

## Licenca

MIT. Consulte [LICENSE](LICENSE).
