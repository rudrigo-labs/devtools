# Relatório de Testes de Integração dos Validadores e Fluxos de Execução

- Data e hora da execução: 2026-03-18 07:28:39 -03:00
- Ambiente: Windows, PowerShell, .NET 10.0
- Escopo: validação de configuração, validação de execução, execução de ferramentas, persistência de configuração e alternância entre ferramentas e configuração.

## Projeto de teste criado

- `src/Tests/DevTools.IntegrationTests/DevTools.IntegrationTests.csproj`
- Incluído na solução: `src/DevTools.slnx`

## Arquivos de teste

- `src/Tests/DevTools.IntegrationTests/Validators/EntityServicesValidatorIntegrationTests.cs`
- `src/Tests/DevTools.IntegrationTests/Validators/SnapshotEngineValidatorIntegrationTests.cs`
- `src/Tests/DevTools.IntegrationTests/Execution/ExecutionAndConfigurationIntegrationTests.cs`
- `src/Tests/DevTools.IntegrationTests/Execution/RemainingToolsExecutionAndConfigurationIntegrationTests.cs`

## Casos cobertos

1. `SnapshotUpsertComConfiguracaoInvalidaNaoPersiste`
2. `SnapshotUpsertComConfiguracaoValidaPersiste`
3. `HarvestUpsertComConfiguracaoInvalidaNaoPersiste`
4. `OrganizerUpsertComConfiguracaoInvalidaNaoPersiste`
5. `MigrationsUpsertComConfiguracaoInvalidaNaoPersiste`
6. `SshTunnelUpsertComConfiguracaoInvalidaNaoPersiste`
7. `NgrokUpsertComConfiguracaoInvalidaNaoPersiste`
8. `ExecuteAsyncComOutputBasePathVazioRetornaFalhaDeValidacao`
9. `ExecuteAsyncComMaxFileSizeAcimaDoLimiteRetornaFalhaDeValidacao`
10. `SnapshotDeveSalvarConfigurarExecutarEAlternarComSucesso`
11. `HarvestDeveSalvarConfigurarExecutarEAlternarComSucesso`
12. `OrganizerDeveSalvarConfigurarExecutarEAlternarComSucesso`
13. `MigrationsDeveSalvarConfigurarExecutarEAlternarComSucesso`
14. `SshTunnelDeveSalvarConfigurarExecutarEAlternarComSucesso`
15. `NgrokDeveSalvarConfigurarExecutarEAlternarComSucesso`
16. `RenameDeveExecutarEAlternarConfiguracaoDeExecucaoComSucesso`
17. `SearchTextDeveExecutarEAlternarPadroesComSucesso`
18. `Utf8ConvertDeveExecutarEAlternarBomComSucesso`
19. `ImageSplitDeveExecutarEAlternarFiltroDeRegiaoComSucesso`

## Comandos executados

```powershell
dotnet test src/Tests/DevTools.IntegrationTests/DevTools.IntegrationTests.csproj -nologo
dotnet test src/DevTools.slnx -nologo
```

## Resultado

- Total de testes: 19
- Aprovados: 19
- Falhas: 0
- Ignorados: 0

Resumo observado:
- Execução no projeto de teste: `Aprovado! – Com falha: 0, Aprovado: 19, Ignorado: 0, Total: 19`
- Execução na solução: `Aprovado! – Com falha: 0, Aprovado: 19, Ignorado: 0, Total: 19`

## Observações

- Os testes de integração confirmam que os validadores impedem persistência e execução com configuração inválida.
- Os testes de execução e alternância validam consistência de estado inicial ao navegar entre configuração e ferramentas.
- Os testes de alternância registram detalhes no arquivo: `docs/tests/resultado-testes-integracao-execucao-configuracao.log`.
- O módulo `Notes` não foi incluído neste ciclo, conforme direcionamento funcional vigente.
