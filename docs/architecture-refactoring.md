# Reestruturação do Motor (V2) – Arquitetura Modular

Status: Concluido (2026-02-07)

## 1. O Porquê da Mudança (Contexto)

O **DevTools** nasceu como uma coleção de scripts de console simples. Com o crescimento da suíte e a necessidade de suportar múltiplas interfaces (CLI, Tray/WPF e Menu de Contexto do Windows), a arquitetura atual apresenta um acoplamento indesejado: a lógica de negócio ("Como fazer") está misturada com a lógica de apresentação ("Como mostrar").

### O Problema Atual
Hoje, para criar uma interface gráfica (WPF) para o `Snapshot`, teríamos que duplicar código ou fazer "gambiarras" para capturar a saída do Console. Isso viola o princípio de **Single Responsibility** e dificulta testes automatizados.

### A Solução V2
Adotaremos uma arquitetura de **Separação de Responsabilidades (SoC)** estrita, mas pragmática. Não usaremos uma "Arquitetura Hexagonal/DDD" complexa e corporativa, pois isso traria *over-engineering* para ferramentas utilitárias.

Em vez disso, usaremos o conceito de **Engines Isoladas**.

---

## 2. Filosofia da Nova Arquitetura

1.  **O Motor é Rei:** A lógica de execução (o "Motor") não deve saber que existe um Console ou uma Janela WPF. Ela recebe dados, processa e retorna um `Result`.
2.  **Zero UI no Core:** É proibido usar `Console.WriteLine`, `MessageBox.Show` ou cores dentro das bibliotecas Core.
3.  **Result Pattern:** Toda operação retorna um objeto `Result<T>` (Sucesso/Falha), garantindo que quem chamou (CLI ou WPF) decida como mostrar o erro.

---

## 3. Padrão de Pastas e Projetos (O "Shape" da Library)

Cada ferramenta (ex: `DevTools.Snapshot`, `DevTools.Organizer`) deixará de ser um projeto único e passará a ser uma solução composta, ou pelo menos, terá suas responsabilidades segregadas internamente se mantivermos um projeto só (embora a separação física em projetos seja recomendada para garantir o desacoplamento).

A estrutura canônica para uma ferramenta na V2 será:

### 📂 `src/DevTools/DevTools.NomeDaFerramenta/`

#### 📦 1. `DevTools.NomeDaFerramenta.Core` (Class Library)
Este é o "Cérebro". Deve ser .NET Standard ou .NET Core puro, sem dependências de UI.

*   **`Models/`**: Classes de dados (DTOs) e Opções.
    *   Ex: `SnapshotOptions.cs` (record com bools: Json, Html, Txt).
    *   Ex: `SnapshotResult.cs` (caminho do arquivo gerado, estatísticas).
*   **`Engine/`**: A classe que executa a lógica.
    *   Ex: `SnapshotEngine.cs`.
    *   Método: `public Result<SnapshotResult> Execute(SnapshotOptions options)`.
*   **`Abstractions/`** (Opcional): Interfaces se houver necessidade de mockar I/O.
    *   Ex: `IFileScanner`.
*   **`Constants/`**: Strings fixas, templates, regexes. Nada de "magic strings" soltas.

#### 🖥️ 2. `DevTools.NomeDaFerramenta.Cli` (Console App / Plugin)
Esta é a "Boca" que fala com o terminal.

*   **`Commands/`**: Mapeamento do `System.CommandLine`.
    *   Traduz `args[]` para `SnapshotOptions`.
    *   Chama `SnapshotEngine.Execute()`.
    *   **Responsabilidade:** Imprimir barra de progresso, cores e lidar com `Console.Out`.

#### 🖼️ 3. `DevTools.NomeDaFerramenta.Wpf` (User Control / Form)
Esta é a "Cara" que aparece na bandeja.

*   **`Views/`**: O XAML do formulário.
*   **`ViewModels/`**: O estado da tela.
    *   Binda os Checkboxes para `SnapshotOptions`.
    *   Chama `SnapshotEngine.Execute()`.
    *   **Responsabilidade:** Mostrar notificações Toast, abrir janelas e diálogos de arquivo.

---

## 4. Fluxo de Execução (Exemplo Prático)

### Cenário: Gerar Snapshot

1.  **Entrada:**
    *   **Via CLI:** Usuário digita `devtools snapshot --json`.
    *   **Via WPF:** Usuário clica no Checkbox "JSON" e aperta "Gerar".

2.  **Processamento (Core):**
    *   Ambos instanciam `SnapshotEngine`.
    *   Ambos criam um `new SnapshotOptions { GenerateJson = true }`.
    *   Chamam `engine.Execute(options)`.

3.  **Saída:**
    *   O Engine retorna `Result.Success("c:\temp\output.json")`.
    *   **CLI:** Imprime "✅ Arquivo gerado em..." (Verde).
    *   **WPF:** Mostra um Toast Notification "Sucesso" e abre a pasta.

## 5. Benefícios

*   **Testabilidade:** Podemos testar o `Engine` com testes unitários rápidos (xUnit) sem precisar abrir console ou janelas.
*   **Consistência:** A lógica de "como varrer os arquivos" é idêntica nas duas interfaces.
*   **Manutenção:** Se mudarmos a regra de negócio (ex: ignorar pasta `node_modules`), mudamos apenas no Core e reflete em tudo.

---
*Documento gerado para a refatoração V2.*
