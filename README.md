# 🚀 DevTools

> Ecossistema modular de ferramentas .NET com foco em reutilização de engines e integração com Presentation Framework (WPF / WWPF).

---

## 📌 Status Atual do Projeto

O DevTools está em evolução.

Atualmente:

* ✅ As libraries (engines) são o núcleo estável do projeto
* ⚙️ A WPF (WWPF / Tray) está em expansão
* 🚧 O Console ainda não está completo

A prioridade atual é consolidar a **WPF como launcher inteligente**, mantendo as engines independentes.

---

## 🧠 Conceito Central

O DevTools é composto exclusivamente por **class libraries**.

Essas libraries contêm apenas:

* Engines
* Modelos
* Validações
* IO
* Regras internas da ferramenta

Elas **não contêm**:

* Console
* UI
* Código de apresentação

A interface (WPF, CLI, Web, etc.) apenas consome as engines.

---

## 🖥️ WPF (Presentation Framework – WWPF)

A WPF é a camada gráfica do DevTools.

Ela:

* Atua como launcher
* Organiza fluxos complexos
* Reduz fricção de uso
* Permite cenários visuais (preview, seleção, acompanhamento de execução)

A WPF **não contém regra de negócio**.
Ela apenas orquestra chamadas às engines.

Arquiteturalmente:

```
WPF (WWPF / Tray)
        ↓
DevTools.* (Engines)
        ↓
DevTools.Core
```

---

## 🏗️ Estrutura da Solution

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

### Regra obrigatória

> Toda tool referencia **DevTools.Core**.

---

## 🔹 DevTools.Core

Contém apenas:

* Contratos globais
* Result models (RunResult, ErrorDetail)
* Interfaces compartilhadas
* Estruturas neutras

Core é mínimo. Nada de lógica específica de ferramenta.

---

## 🔹 Engines

Cada tool possui:

* Uma classe principal (Engine)
* Um método padrão de execução
* Resultado padronizado

Exemplo:

```csharp
public class SnapshotEngine
{
    public async Task<RunResult> ExecuteAsync(SnapshotOptions options)
    {
        // lógica da tool
    }
}
```

---

## 🎯 Direção do Projeto

* Engines como base sólida
* WPF como interface principal
* Console como interface secundária (em construção)
* Host agnóstico
* Expansão incremental por ferramenta

---

## 📌 Decisão para o GitHub

Este repositório será mantido como:

> **Monorepo de engines (libraries-only)**.

A WPF e outras camadas de apresentação podem evoluir separadamente, mas sempre consumindo estas libraries.

O foco do GitHub é consolidar o núcleo reutilizável do DevTools.

---

## 📄 Licença

Definir conforme estratégia futura do projeto.
