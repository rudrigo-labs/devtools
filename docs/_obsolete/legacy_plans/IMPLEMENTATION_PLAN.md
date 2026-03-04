# Plano de Implementação: Gestão de Perfis e Configurações

Este documento serve como guia para a implementação da funcionalidade de perfis padrão no DevTools.

## 1. Objetivo
Permitir que cada ferramenta (Rename, Migrations, etc.) tenha múltiplos perfis salvos, com a capacidade de definir um deles como "Padrão" (`IsDefault`). Se houver um padrão, a ferramenta abre preenchida; caso contrário, abre vazia.

## 2. Estrutura de Dados (Modelos)
- Localizar os modelos de perfil (ex: `RenameProfile`, `MigrationsSettings`).
- Adicionar a propriedade `bool IsDefault { get; set; }`.
- No `ProfileManager.cs`, garantir que ao marcar um perfil como padrão, todos os outros da mesma ferramenta sejam desmarcados (`IsDefault = false`).

## 3. Interface do Usuário (MainWindow - Aba Settings)
- **Separação Visual**: Dividir a aba de configurações em duas áreas:
    1. **Ferramentas (Global)**: Configurações fixas (ex: caminhos do Ngrok, regras do Harvest).
    2. **Gestão de Perfis**: Lista de perfis criados pelo usuário para cada ferramenta.
- **Editor de Perfis**:
    - Lista lateral (ListBox) com os nomes dos perfis.
    - Formulário de edição com os campos específicos da ferramenta.
    - CheckBox: "Usar este perfil como padrão".
    - Botões: [Novo], [Salvar], [Excluir].

## 4. Integração com as Ferramentas
- No evento `OnLoaded` de cada janela (ex: `RenameWindow.xaml.cs`):
    - Chamar `ProfileManager.GetDefaultProfile<T>(toolName)`.
    - Se retornar um perfil, preencher automaticamente todos os campos da UI.

## 5. Próximos Passos
1. Atualizar modelos de dados.
2. Refatorar `ProfileManager`.
3. Redesenhar `MainWindow.xaml` (aba Settings).
4. Implementar a lógica de carregamento automático nas janelas filhas.
