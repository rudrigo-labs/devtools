# Lista de MessageBox na aplicaÃ§Ã£o

- Total: 12
- Projeto: Presentation/DevTools.Presentation.Wpf

## Diretos em SettingsTabView.xaml.cs
- [SettingsTabView.xaml.cs:L130](file:///c:/Users/rodrigo/Documents/Projetos/rudrigo-labs/devtools/src/Presentation/DevTools.Presentation.Wpf/Views/SettingsTabView.xaml.cs#L130)

```csharp
System.Windows.MessageBox.Show("ConfiguraÃ§Ã£o salva com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
```

- [SettingsTabView.xaml.cs:L138](file:///c:/Users/rodrigo/Documents/Projetos/rudrigo-labs/devtools/src/Presentation/DevTools.Presentation.Wpf/Views/SettingsTabView.xaml.cs#L138)

```csharp
if (System.Windows.MessageBox.Show($"Tem certeza que deseja excluir o configuracao '{_selectedConfiguration.Name}'?", "Confirmar ExclusÃ£o", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
```

- [SettingsTabView.xaml.cs:L225](file:///c:/Users/rodrigo/Documents/Projetos/rudrigo-labs/devtools/src/Presentation/DevTools.Presentation.Wpf/Views/SettingsTabView.xaml.cs#L225)

```csharp
System.Windows.MessageBox.Show("ConfiguraÃ§Ã£o do Harvest salva com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
```

- [SettingsTabView.xaml.cs:L229](file:///c:/Users/rodrigo/Documents/Projetos/rudrigo-labs/devtools/src/Presentation/DevTools.Presentation.Wpf/Views/SettingsTabView.xaml.cs#L229)

```csharp
System.Windows.MessageBox.Show($"Erro ao salvar: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
```

- [SettingsTabView.xaml.cs:L296](file:///c:/Users/rodrigo/Documents/Projetos/rudrigo-labs/devtools/src/Presentation/DevTools.Presentation.Wpf/Views/SettingsTabView.xaml.cs#L296)

```csharp
System.Windows.MessageBox.Show("Categoria salva!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
```

- [SettingsTabView.xaml.cs:L304](file:///c:/Users/rodrigo/Documents/Projetos/rudrigo-labs/devtools/src/Presentation/DevTools.Presentation.Wpf/Views/SettingsTabView.xaml.cs#L304)

```csharp
if (System.Windows.MessageBox.Show($"Excluir categoria '{_selectedCategory.Name}'?", "Confirmar", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
```

- [SettingsTabView.xaml.cs:L341](file:///c:/Users/rodrigo/Documents/Projetos/rudrigo-labs/devtools/src/Presentation/DevTools.Presentation.Wpf/Views/SettingsTabView.xaml.cs#L341)

```csharp
System.Windows.MessageBox.Show("ConfiguraÃ§Ãµes do Migrations salvas!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
```

- [SettingsTabView.xaml.cs:L372](file:///c:/Users/rodrigo/Documents/Projetos/rudrigo-labs/devtools/src/Presentation/DevTools.Presentation.Wpf/Views/SettingsTabView.xaml.cs#L372)

```csharp
System.Windows.MessageBox.Show("ConfiguraÃ§Ãµes do Ngrok salvas!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
```

## Via UiMessageService
- [UiMessageService.cs:L15](file:///c:/Users/rodrigo/Documents/Projetos/rudrigo-labs/devtools/src/Presentation/DevTools.Presentation.Wpf/Services/UiMessageService.cs#L15)

```csharp
System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
```

- [UiMessageService.cs:L20](file:///c:/Users/rodrigo/Documents/Projetos/rudrigo-labs/devtools/src/Presentation/DevTools.Presentation.Wpf/Services/UiMessageService.cs#L20)

```csharp
System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
```

-- [UiMessageService.cs:L25-L26](file:///c:/Users/rodrigo/Documents/Projetos/rudrigo-labs/devtools/src/Presentation/DevTools.Presentation.Wpf/Services/UiMessageService.cs#L25-L26)

```csharp
var mb = System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
return mb == MessageBoxResult.Yes;
```

