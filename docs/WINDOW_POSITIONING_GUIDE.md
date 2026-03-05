# Guia de Posicionamento de Janelas (DevTools Hub)

Este documento explica como o DevTools Hub gerencia a posição das janelas das ferramentas e como garantir que elas abram sempre no canto inferior direito da tela, mesmo sendo janelas "filhas" da MainWindow.

## 1. O Serviço de Bandeja (TrayService)
O posicionamento centralizado é gerenciado no arquivo `TrayService.cs`, dentro do método genérico `ShowWindow<T>`.

### Comportamento Desejado
- Todas as janelas de ferramentas devem ter a `MainWindow` como sua `Owner` (mãe).
- Todas as janelas devem se posicionar no canto inferior direito da tela de trabalho (WorkArea).

## 2. Implementação Técnica

Para garantir esse comportamento, o código no `TrayService.cs` segue este padrão:

```csharp
// 1. Define a MainWindow como dona para que a ferramenta fique sempre à frente
if (_mainWindow != null && window != _mainWindow)
{
    window.Owner = _mainWindow;
    window.ShowInTaskbar = false;
    
    // IMPORTANTE: Definir como Manual impede que o WPF force a centralização
    window.WindowStartupLocation = WindowStartupLocation.Manual;
}

// 2. Lógica de posicionamento no evento Loaded
window.Loaded += (s, e) =>
{
    var screen = SystemParameters.WorkArea;
    // Posiciona a janela considerando sua largura e altura atuais
    window.Left = screen.Right - window.ActualWidth - 20;
    window.Top = screen.Bottom - window.ActualHeight - 20;
};
```

## 3. Por que o Organizador era diferente?
Inconsistências no posicionamento geralmente ocorrem por:
1.  **WindowStartupLocation**: Se estiver definido como `CenterOwner` ou `CenterScreen`, a lógica manual no `Loaded` pode ser sobrescrita ou causar um efeito de "pulo" na janela.
2.  **Ordem de Execução**: O evento `Loaded` é o momento ideal porque a janela já calculou seu `ActualWidth` e `ActualHeight` baseados no conteúdo.

## 4. Como ajustar novas janelas
Ao criar uma nova ferramenta, certifique-se de que o `TrayService` a instancie através do método `ShowWindow`, passando as dependências necessárias no construtor. A lógica de posicionamento já está embutida no `ShowWindow` e será aplicada automaticamente a todas as novas janelas.
