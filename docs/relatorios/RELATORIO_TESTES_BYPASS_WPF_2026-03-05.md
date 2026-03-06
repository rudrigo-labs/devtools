# RELATORIO DE TESTES - BYPASS DE TESTES WPF SKIPPED

Data: 2026-03-05  
Projeto: DevTools (WPF + Tools)  
Solicitacao: repetir os testes, tentar executar os 2 casos marcados como `Skip`, e registrar tudo de forma detalhada.

## 1. Escopo executado

Foram executados:

1. Teste de simulacao completa de uso das ferramentas (`ToolUsageSimulationTests`).
2. Suite completa de testes do projeto `DevTools.Tests`.
3. Suite de testes pela solucao (`src/DevTools.slnx`).
4. Investigacao e execucao "bypass" dos 2 testes originalmente marcados com `[Fact(Skip = ...)]`:
   - `PathSelectorTests.SelectedPath_Updates_TextBox_Display`
   - `SnapshotWindowTests.ProcessButton_Persists_SelectedPath_To_Settings`

## 2. Diagnostico dos testes pulados

Os dois testes estao pulados por `Skip` no codigo-fonte, com motivo:

- "Instavel em xUnit por afinidade de thread do Application.Current e recursos WPF globais."

Arquivos:

- `src/Tools/DevTools.Tests/PathSelectorTests.cs`
- `src/Tools/DevTools.Tests/SnapshotWindowTests.cs`

## 3. Estrategia de bypass aplicada

Como `Skip` impede execucao direta via `dotnet test`, foi feita execucao bypass controlada:

1. Criacao temporaria de um teste auxiliar em STA com Dispatcher para reproduzir os 2 cenarios no mesmo contexto WPF.
2. Execucao do teste auxiliar via filtro.
3. Validacao dos resultados.
4. Remocao do arquivo temporario para nao poluir a suite oficial.

Observacao: esse bypass foi ad-hoc (investigativo), sem alterar os testes oficiais marcados com `Skip`.

## 4. Linha do tempo de execucao

### 4.1 Simulacao completa de uso

Comando:

```powershell
dotnet test src/Tools/DevTools.Tests/DevTools.Tests.csproj -v minimal --filter "FullyQualifiedName~ToolUsageSimulationTests"
```

Resultado final: **PASSOU**.

### 4.2 Suite `DevTools.Tests` completa

Comando:

```powershell
dotnet test src/Tools/DevTools.Tests/DevTools.Tests.csproj -v minimal
```

Resultado final estavel: **PASSOU** com:

- 36 aprovados
- 2 ignorados (os 2 WPF com `Skip`)
- 0 falhas

### 4.3 Suite da solucao

Comando:

```powershell
dotnet test src/DevTools.slnx -v minimal
```

Resultado final: **PASSOU** com mesmos numeros do pacote de testes.

### 4.4 Execucao bypass dos 2 skipped

Comando (bypass temporario):

```powershell
dotnet test src/Tools/DevTools.Tests/DevTools.Tests.csproj -v minimal --filter "FullyQualifiedName~SkippedWpfBypassIntegrationTests"
```

Resultado final do bypass: **PASSOU** (1 teste consolidado, cobrindo os 2 cenarios skipped).

## 5. Incidentes durante a execucao

Foi observado 1 incidente transitorio durante uma rodada intermediaria:

- cancelamento abrupto do `testhost` com assert interno de CLR (`Attempt to execute managed code after the .NET runtime thread state has been destroyed`).

Acao aplicada:

- reexecucao limpa da suite com parametros de diagnostico e sem paralelismo de acoes manuais concorrentes.

Resultado:

- suites voltaram a estado estavel (passando).

## 6. Resultado consolidado

Status atual:

1. Simulacao principal de integracao: **OK**.
2. Suite oficial de testes: **OK** (com 2 skipped esperados).
3. Solucao completa: **OK**.
4. Dois cenarios skipped: **executados com bypass ad-hoc e validados**.

## 7. Conclusao tecnica

- O projeto esta com execucao de testes estavel no estado atual.
- Os 2 testes skipped continuam oficialmente skipped por decisao tecnica de estabilidade WPF/xUnit.
- Mesmo assim, os cenarios foram validados por bypass controlado nesta rodada.

## 8. Recomendacao objetiva

Para tornar esses 2 testes permanentes sem `Skip`, o caminho recomendado e separar infraestrutura WPF de teste em uma fixture/processo serial dedicado (STA unico por colecao), evitando disputa de `Application.Current` entre testes.
