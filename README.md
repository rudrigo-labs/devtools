# DevTools

Suite desktop para produtividade tecnica no dia a dia de desenvolvimento.

## Aviso

Este projeto esta em construcao e passando por reestruturacao interna.
Funcionalidades, arquitetura e documentacao podem mudar entre versoes.
Nao considerar o estado atual como release final estabilizada.

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

- Nova fase: [docs/README.md](docs/README.md)
- Arquitetura canonica atual: [docs/architecture.md](docs/architecture.md)
- Registro de demandas (nova fase): [docs/controle/REGISTRO_DEMANDAS.md](docs/controle/REGISTRO_DEMANDAS.md)
- Historico/documentacao anterior: [legacy-v1/docs/README.md](legacy-v1/docs/README.md)

## Licenca

MIT. Consulte [LICENSE](LICENSE).
