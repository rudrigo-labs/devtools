# DevTools - Documentação Técnica

## Visão Geral
O **DevTools** é uma aplicação WPF modular construída sobre .NET 8/9, projetada para aumentar a produtividade do desenvolvedor. A arquitetura segue princípios de **Clean Architecture** simplificada e **Pure Dependency Injection** (sem contêineres complexos), priorizando performance e baixo acoplamento.

## Arquitetura

### Estrutura da Solução
- **DevTools.Core:** Abstrações (`IDevToolEngine`), Modelos e Interfaces comuns.
- **DevTools.Presentation.Wpf:** Camada de apresentação (UI), ponto de entrada.
- **Tools Projects:** Cada ferramenta possui seu próprio projeto/biblioteca (ex: `DevTools.Rename`, `DevTools.Organizer`) contendo a lógica de negócio (Engine).

### Componentes Chave

#### 1. TrayService (Central Controller)
Gerencia o ciclo de vida da aplicação na bandeja do sistema.
- Responsável por instanciar e abrir janelas de ferramentas.
- Controla o ícone da bandeja e o menu de contexto.
- Atua como "Launcher" para o Dashboard e janelas individuais.

#### 2. JobManager (Async Task Orchestrator)
Gerencia a execução de tarefas em background para não bloquear a UI.
- Mantém uma coleção observável de `UiJob`.
- Suporta cancelamento via `CancellationToken`.
- Fornece eventos de progresso e logs em tempo real.

#### 3. IDevToolEngine
Interface padrão para todas as ferramentas "executáveis" (que rodam jobs).
```csharp
public interface IDevToolEngine
{
    Task<IResult> ExecuteAsync(IRequest request, IProgressReporter progress, CancellationToken ct);
}
```

#### 4. SettingsService
Gerencia a persistência de configurações do usuário (posições de janela, últimos caminhos) via JSON local.

## Como Adicionar uma Nova Ferramenta

1. **Core/Logic:**
   - Crie um novo projeto ou pasta em `src/Tools`.
   - Implemente a lógica de negócio (se complexa, implemente `IDevToolEngine`).

2. **UI (WPF):**
   - Crie uma nova `Window` em `Views/`.
   - Injete `JobManager` e `SettingsService` no construtor se necessário.

3. **Integração:**
   - Adicione a entrada no `TrayResources.xaml` (Menu).
   - Adicione o botão no `DashboardWindow.xaml` (Card).
   - Registre a abertura no `TrayService.cs` (método `OpenTool`).

## Detalhes de Implementação

- **Dashboard:** Utiliza `Grid` layout e `TabControl` para organizar ferramentas e jobs. O DataGrid de jobs utiliza DataBinding direto com a coleção do `JobManager`.
- **UI Threading:** O `JobManager` utiliza `SynchronizationContext` (ou Dispatcher) para garantir que atualizações de progresso vindas de threads secundárias sejam marshalled corretamente para a UI.
- **Recursos:** Ícones utilizam `Segoe MDL2 Assets` font ou `System.Drawing.Icon` para a bandeja.
