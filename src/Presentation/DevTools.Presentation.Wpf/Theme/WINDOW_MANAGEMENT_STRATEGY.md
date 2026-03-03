# Estratégia de Gerenciamento de Janelas (Modal-Like)

Este documento descreve como o DevTools Hub gerencia a relação entre a Janela Principal (Dashboard) e as janelas de ferramentas (Filhas), garantindo que elas se comportem de forma sincronizada e profissional.

## 1. O Conceito "Modal-Like"
Para garantir o foco e a integridade da aplicação, adotamos um comportamento onde apenas uma ferramenta pode estar ativa por vez, e o Dashboard fica bloqueado enquanto a ferramenta estiver aberta.

### Regras de Ouro:
- **Bloqueio do Pai**: Ao abrir uma ferramenta, a `MainWindow` (Pai) tem sua propriedade `IsEnabled` definida como `false`. Isso impede cliques, redimensionamentos e interações acidentais.
- **Liberação no Fechamento**: Quando a ferramenta (Filha) é fechada, a `MainWindow` detecta o evento e volta para `IsEnabled = true`.
- **Sincronização de Visibilidade**: As janelas filhas devem seguir rigorosamente o estado de visibilidade do pai. Se o pai for escondido (Hide) ou minimizado, a filha deve fazer o mesmo.

## 2. Implementação no TrayService.cs
O controle é centralizado no método `ShowWindow<T>`.

```csharp
// Ao mostrar a janela:
_mainWindow.IsEnabled = false; // Bloqueia o Dashboard

// Ao fechar a janela (evento Closed):
_mainWindow.IsEnabled = true;  // Libera o Dashboard
_mainWindow.Activate();        // Traz o Dashboard para frente
```

## 3. Por que não usar .ShowDialog()?
Embora o `.ShowDialog()` nativo do WPF seja modal, ele é "bloqueante" no código C#. Isso impediria que o `TrayService` continuasse processando ícones de bandeja ou notificações. A abordagem de desabilitar manualmente o `IsEnabled` do pai nos dá o mesmo efeito visual e funcional de uma modal, mas mantém a aplicação responsiva em segundo plano.

## 4. Sincronização de Minimizar/Fechar
No code-behind da `MainWindow.xaml.cs`, monitoramos as mudanças de estado para replicar nas filhas:
- Se `MainWindow` esconder -> Esconder ferramenta ativa.
- Se `MainWindow` aparecer -> Mostrar ferramenta ativa.
