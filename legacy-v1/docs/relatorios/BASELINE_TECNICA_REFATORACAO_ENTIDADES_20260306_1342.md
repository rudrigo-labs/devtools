# Baseline Tecnica - Refatoracao Entidades Ferramentas

- Data/Hora: 2026-03-06 13:42
- Demanda: `DEFINICAO_REFATORACAO_ENTIDADES_FERRAMENTAS_20260306_1315.md`

## Ambiente
- SDK: .NET 10.0.103
- SO: Windows 10.0.26200 (win-x64)

## Estado do repositorio no baseline
- Total de itens modificados/novos no working tree: 47
- Observacao: baseline capturado em arvore de trabalho ja em andamento (nao limpa).

## Comandos executados
1. `dotnet build src/Presentation/DevTools.Presentation.Wpf/DevTools.Presentation.Wpf.csproj -nologo`
- Resultado: SUCESSO
- Observacao: 1 warning de lock intermitente em `DevTools.Core.dll` (MSB3026), compilacao concluiu.

2. `dotnet build src/Tools/DevTools.Tests/DevTools.Tests.csproj -nologo`
- Resultado: FALHA
- Erro: `CS2012` (arquivo `DevTools.Core.dll` em uso por outro processo / `VBCSCompiler`).

3. `dotnet test src/Tools/DevTools.Tests/DevTools.Tests.csproj -nologo -p:UseSharedCompilation=false -p:BuildInParallel=false -maxcpucount:1`
- Resultado: PARCIAL
- Testes executados: 8/8 aprovados
- Falha final: processo `testhost.exe` abortado por assert do CLR apos execucao dos testes.

## Conclusao da Fase 0
- Baseline tecnico capturado com evidencias de build/teste.
- Existe instabilidade de infraestrutura de teste local (lock de compilacao e abort do host de teste) que deve ser tratada na fase de hardening.
