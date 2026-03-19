# Relatório de Testes de Integração dos Validadores

- Data e hora da execução: 2026-03-17 09:22:58 -03:00
- Ambiente: Windows, PowerShell, .NET 10.0
- Escopo: validação de configuração e validação de execução (Snapshot), com foco no fluxo integrado `Service/Engine -> Validator`.

## Projeto de teste criado

- `src/Tests/DevTools.IntegrationTests/DevTools.IntegrationTests.csproj`
- Incluído na solução: `src/DevTools.slnx`

## Arquivos de teste

- `src/Tests/DevTools.IntegrationTests/Validators/EntityServicesValidatorIntegrationTests.cs`
- `src/Tests/DevTools.IntegrationTests/Validators/SnapshotEngineValidatorIntegrationTests.cs`

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

## Comandos executados

```powershell
dotnet test src/Tests/DevTools.IntegrationTests/DevTools.IntegrationTests.csproj -nologo
dotnet test src/DevTools.slnx -nologo
```

## Resultado

- Total de testes: 9
- Aprovados: 9
- Falhas: 0
- Ignorados: 0

Resumo observado:
- Execução no projeto de teste: `Aprovado! – Com falha: 0, Aprovado: 9, Ignorado: 0, Total: 9`
- Execução na solução: `Aprovado! – Com falha: 0, Aprovado: 9, Ignorado: 0, Total: 9`

## Observações

- Os testes validam que configurações inválidas não são persistidas nos serviços das ferramentas.
- Os testes de Snapshot validam que o `SnapshotEngine` retorna falha quando o `SnapshotRequest` viola regras de validação.
- O módulo `Notes` não foi incluído neste ciclo, conforme direcionamento funcional vigente.
