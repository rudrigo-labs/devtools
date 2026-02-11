# DevTools — Guia do projeto (libraries somente)

Status: Concluido (2026-02-07)

Este documento define **como o projeto DevTools deve ser estruturado e implementado**, considerando **somente libraries**. Ele serve como **especificação única** para implementação (inclusive para uso com Codex).

---

## Regras (obrigatórias)

- **Somente libraries** (`classlib`).
- **Nenhuma CLI, nenhuma UI, nenhum Console** dentro das libraries.
- Cada ferramenta possui **um motor** (uma classe principal) e **um método padrão de execução**.
- Existe **um único projeto compartilhado** para itens globais (contratos, resultados, abstrações neutras).
- Tudo fica **na raiz da solution** (sem `src/`, sem folders de solution).

---

## Estrutura da solution (na raiz)

```
DevTools.slnx
DevTools.Core
DevTools.Snapshot
DevTools.Organizer
DevTools.Ngrok
DevTools.SSHTunnel
DevTools.Harvest
DevTools.Notes
DevTools.Rename
DevTools.SearchText
DevTools.Migrations
DevTools.Utf8Convert
DevTools.Image
```

**Regra:** toda tool **DEVE** referenciar `DevTools.Core`.

---

## DevTools.Core — escopo e responsabilidade

### Objetivo
Ser o **miolo global** compartilhado por todas as tools.

### Deve conter (obrigatório)

- **Resultado padrão**
  - `RunResult`
  - `RunResult<T>`
  - `ErrorDetail`

- **Progresso neutro (sem console)**
  - `ProgressEvent`
  - um reporter de progresso (nome livre)

- **Contrato padrão do motor**
  - interface genérica para execução de motores

- **Abstrações neutras mínimas**
  - filesystem
  - execução de processo

- **Guard / validação simples**

### Não pode conter (proibido)

- Qualquer regra de Snapshot, SSH, Ngrok ou outras tools
- Qualquer código de UI ou CLI
- Qualquer uso de `Console`

---

## Padrão interno de cada tool

Dentro de **cada** `DevTools.<Tool>`:

```
Engine/
  motor (regra e orquestração)

Models/
  request / response e modelos auxiliares

Validation/
  validação do request/options

Abstractions/ (opcional)
  somente se a tool precisar de interfaces específicas

Providers/ (opcional)
  implementações técnicas neutras (sem UI)
```

> Se uma tool **não precisar** de `Providers`, **não criar**.

---

## Padrão do motor (classe e método)

Cada tool deve ter:

- **1 classe principal**: `XxxEngine`
- **1 método principal**: `ExecuteAsync(...)`

### Contrato do método

- Entrada: `XxxRequest`
- Retorno: `RunResult<XxxResponse>`
- Recebe `CancellationToken`
- Progresso **opcional** via reporter (sem console)

### Proibição absoluta

- O motor **NUNCA** imprime nada.

---

## O que significa “funciona”

Uma tool é considerada **funcionando** quando:

- Compila
- Possui `Request` e `Response`
- Possui `Engine` com `ExecuteAsync`
- Valida o request
- Executa o trabalho (mesmo que mínimo)
- Retorna sucesso ou erro via `RunResult`

---

## Checklist curto para implementação (Codex)

Copiar exatamente este checklist:

```
Objetivo: DevTools (libraries somente)

1) DevTools.Core:
   - RunResult / RunResult<T> / ErrorDetail
   - ProgressEvent + reporter (sem console)
   - Contrato padrão do motor (genérico)
   - Abstrações mínimas: filesystem + process runner
   - Guard/validação simples
   - Proibido: UI/CLI/Console e regras de tools

2) Para cada tool DevTools.<Tool>:
   - Pastas: Engine, Models, Validation (Abstractions/Providers só se precisar)
   - Criar <Tool>Request e <Tool>Response
   - Criar <Tool>Engine com ExecuteAsync(request, reporter?, ct) -> RunResult<Response>
   - Não usar Console
   - Referenciar DevTools.Core

3) Entrega mínima:
   - Implementar 1 tool completa (motor mínimo) seguindo o padrão
```
