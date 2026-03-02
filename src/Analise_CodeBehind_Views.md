# Análise de Code-Behind e Lógica de Negócio: Atual vs Backup

**Data:** 24/02/2026
**Contexto:** Comparação entre a implementação atual (`src/.../Views`) e o backup fornecido (`bkp-views/Views`) para identificar necessidades de refatoração e validar a maturidade do código atual.

## 1. Visão Geral Executiva

A análise confirma que a versão atual (`src`) é uma **evolução refatorada** da versão de backup. O código atual incorpora melhorias significativas de UI/UX (uso de componentes reutilizáveis como `PathSelector`), validações mais robustas e integração padronizada com o `JobManager`.

**Conclusão Principal:** Não é necessário "restaurar" código do backup. O código atual está funcionalmente à frente. O foco deve ser na **refatoração arquitetural** para remover a lógica de orquestração que persiste no *Code-Behind* de ambas as versões.

---

## 2. Análise Detalhada por View

### 🔍 SearchTextWindow
*   **Status:** ✅ Atual é superior.
*   **Comparação:**
    *   **Backup:** Instanciação direta da Engine no botão. Diálogos de alerta simples.
    *   **Atual:** Mantém a estrutura lógica, mas utiliza `JobManager` para envolver a execução (não trava UI). Validações de input melhoradas.
*   **Acoplamento (Problema):** Em ambas, a View constrói o `SearchTextRequest` e instancia `new SearchTextEngine()`.
*   **Recomendação:** Extrair orquestração para `ISearchTextService`.

### 🌾 HarvestWindow
*   **Status:** ✅ Atual é superior.
*   **Comparação:**
    *   **Backup:** Lógica funcional básica.
    *   **Atual:** Idêntica lógica de negócio, mas com melhor tratamento de erros e validação de caminhos (`Directory.Exists`) antes da execução.
*   **Acoplamento (Problema):** A View conhece `HarvestEngine` e suas dependências.
*   **Recomendação:** Criar `IHarvestAppService` para encapsular a chamada da engine.

### 🚇 SshTunnelWindow
*   **Status:** ✅ Atual é equivalente/superior.
*   **Comparação:**
    *   **Backup:** Lógica de polling (Timer) para status.
    *   **Atual:** Mantém a lógica de polling e validação. Melhor componentização dos inputs.
*   **Observação:** Ambas versões instanciam `TunnelService` manualmente (`new TunnelService(...)`), o que quebra a Injeção de Dependência ideal.
*   **Recomendação:** Registrar `ITunnelService` no container de DI e injetar no construtor, removendo o `new` do code-behind.

### 🌐 NgrokWindow
*   **Status:** ✅ Atual é superior (mais robusto).
*   **Comparação:**
    *   **Backup:** Focado em lista de túneis e ações simples.
    *   **Atual:** Suporta configurações avançadas (AuthToken, Subdomain) e diferentes ações via `ActionCombo`.
*   **Acoplamento:** A lógica de "construir argumentos CLI" (`extraArgs`) está misturada com a lógica de UI.
*   **Recomendação:** Mover a construção de parâmetros para um `NgrokCommandBuilder` ou similar.

### 🖼️ ImageSplitWindow
*   **Status:** ✅ Atual é muito superior.
*   **Comparação:**
    *   **Backup:** Código verboso com `OpenFileDialog` e `FolderBrowserDialog` poluindo o code-behind.
    *   **Atual:** Limpo. Usa `PathSelector` (componente reutilizável) para inputs de arquivo/pasta.
*   **Recomendação:** Manter atual. Refatorar apenas a chamada da Engine para serviço.

### 🏷️ RenameWindow
*   **Status:** ✅ Atual é superior.
*   **Comparação:**
    *   **Mesmo cenário do ImageSplit:** O backup tem muita lógica de diálogo de arquivo no code-behind. O atual delega isso para controles de UI dedicados.

---

## 3. Diagnóstico de Arquitetura (O que falta?)

Embora o código atual seja melhor, ele herdou o **vício arquitetural** do backup: **Acoplamento View-Engine**.

**Padrão Atual (Code-Behind):**
```csharp
private async void Executar_Click(...) {
    // 1. Validação de UI (Ok estar aqui)
    if (invalido) return;

    // 2. Construção do Request (Lógica de Apresentação -> Domínio)
    var request = new Request(...);

    // 3. Instanciação da Engine (ACOPLAMENTO FORTE)
    var engine = new Engine(); 

    // 4. Execução
    var result = await engine.Execute(request);

    // 5. Atualização de UI (Ok estar aqui)
    Mostrar(result);
}
```

**Padrão Desejado (MVVM + Services):**
```csharp
// Injetado no Construtor
private readonly IToolService _service; 

private async void Executar_Click(...) {
    // 1. Validação UI
    // 2. Execução via Serviço (Abstração)
    var result = await _service.ExecuteAsync(viewModel.ToRequest());
    // 3. Atualização UI
}
```

## 4. Plano de Ação Sugerido

1.  **Não restaurar nada do backup.** O código `src` já é a versão definitiva e melhorada.
2.  **Prioridade 1:** Padronizar a injeção de serviços. (Ex: `SshTunnelWindow` não deveria dar `new TunnelService`).
3.  **Prioridade 2:** Para cada ferramenta, criar uma interface de serviço simples (ex: `IImageSplitterService`) que envolva a Engine.
4.  **Prioridade 3:** Mover a lógica de validação de negócio (ex: "se alpha < 0") para dentro do Request ou do Serviço, tirando do Code-Behind.
