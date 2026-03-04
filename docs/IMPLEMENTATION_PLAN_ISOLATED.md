# Plano de Implementação: Perfis de Ferramentas (Abordagem Isolada)

Este documento descreve a estratégia de implementação da funcionalidade de perfis, com foco em isolamento para garantir a estabilidade do código existente.

## Princípio Guia
A nova funcionalidade será desenvolvida em um "laboratório" isolado. Apenas após estar 100% funcional e validada, ela será conectada, de forma cirúrgica e mínima, ao restante da aplicação.

---

### Fase 1: A Base de Dados (Alteração Mínima e Segura)

O único pré-requisito é que nosso modelo de dados entenda o que é um perfil "padrão".

1.  **Ação:** Modificar **um único arquivo**: `src/Core/DevTools.Core/Models/ToolProfile.cs`.
2.  **Mudança:** Adicionar a propriedade `public bool IsDefault { get; set; }`.
3.  **Impacto:** Zero. Esta alteração por si só não afeta nenhuma lógica existente.

---

### Fase 2: O Cérebro da Funcionalidade (Implementação 100% Isolada)

Criaremos um novo serviço que será o "cérebro" de toda a gestão de perfis.

1.  **Ação:** Criar um **novo arquivo**: `src/Presentation/DevTools.Presentation.Wpf/Services/ProfileUIService.cs`.
2.  **Responsabilidades deste novo serviço:**
    *   **Gerenciar a Lógica de "Padrão":** Conterá um método `SaveProfile(ToolProfile profile)` que, ao receber um perfil com `IsDefault = true`, irá garantir que todos os outros perfis da mesma ferramenta sejam desmarcados.
    *   **Gerar a UI Dinamicamente:** Terá um método `GenerateUIForProfile(string toolName, StackPanel container, ToolProfile profile)` que criará os controles de UI corretos para cada ferramenta.

---

### Fase 3: A Conexão (Integração Cirúrgica e Controlada)

Com o "cérebro" pronto, vamos conectá-lo à interface com o mínimo de alterações.

1.  **Alterar `MainWindow.xaml`:**
    *   Adicionar a nova seção "Perfis de Ferramentas" e o painel de edição.

2.  **Alterar `MainWindow.xaml.cs`:**
    *   Injetar o novo `ProfileUIService`.
    *   Os métodos de clique (`OpenToolProfiles_Click`, `SaveToolProfile_Click`) apenas delegarão a chamada para o `ProfileUIService`.

3.  **Alterar as Janelas das Ferramentas (Ex: `RenameWindow.xaml.cs`):**
    *   **Ação Crítica:** Adicionar um **construtor público vazio** (`public RenameWindow()`) para garantir que o designer do editor não quebre.
    *   No construtor principal, adicionar a injeção do `ProfileManager`.
    *   No evento `OnLoaded`, adicionar a chamada `_profileManager.GetDefaultProfile()` para preencher os campos.

---

### Fase 4: Validação Final

Após a integração, faremos uma verificação completa do fluxo:
1.  Criar e editar perfis.
2.  Definir um perfil como padrão e verificar se a ferramenta carrega os dados.
3.  Garantir que apenas um perfil pode ser padrão por vez.
4.  Excluir perfis.
