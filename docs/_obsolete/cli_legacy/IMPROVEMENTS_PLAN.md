# Plano de Melhorias: Centralização de Configurações no Console (CLI)

Este documento descreve o plano para permitir que o Console (CLI) utilize as mesmas configurações gerenciadas pela interface gráfica (WPF), eliminando a necessidade de entrada manual de dados repetitivos e oferecendo menus de seleção simples.

## Objetivo
Unificar a gestão de configurações entre WPF e CLI. Ao abrir uma ferramenta no console (como SSH ou Harvest), o usuário deverá ver um menu com os perfis já salvos, em vez de ter que digitar parâmetros manualmente ("ardis").

## Estratégia de Implementação

### 1. Arquitetura e Refatoração (Core)
Atualmente, o serviço de leitura de configurações (`ConfigService`) está acoplado ao projeto WPF. Ele deve ser movido para uma camada comum.

*   **Mover `ConfigService`:**
    *   Transferir a classe `ConfigService` de `DevTools.Presentation.Wpf` para `DevTools.Core` (ou namespace apropriado em Core).
    *   Garantir que o serviço seja genérico e não dependa de bibliotecas de UI.
    *   Extrair as classes de modelo de configuração (ex: `SshConfigSection`, `HarvestConfig`) para seus respectivos projetos de domínio ou para o Core, evitando dependências circulares.

*   **Padronização do Caminho do Arquivo:**
    *   Definir um local único para o `appsettings.json` que seja acessível tanto pelo WPF quanto pelo CLI.
    *   Caminho sugerido: `%APPDATA%\DevTools\appsettings.json`.
    *   Ambos os projetos devem ler e gravar neste mesmo local.

### 2. Implementação no Console (CLI)
Adaptar os comandos do Console para consumir o `ConfigService`.

*   **Injeção de Dependência:**
    *   Registrar o `ConfigService` no container de injeção de dependência do `Program.cs` da CLI.
    *   Injetar o serviço nos comandos existentes (`SshTunnelCliCommand`, `HarvestCliCommand`, `OrganizerCliCommand`, etc.).

*   **Refatoração dos Comandos (Fluxo de Menu):**
    *   **SSH Tunnel:**
        *   Carregar perfis salvos via `ConfigService`.
        *   Exibir menu numerado com os perfis (ex: "1. Produção", "2. Homologação").
        *   Permitir seleção rápida ("Menu -> Selecionou -> Pronto").
        *   Manter opção "Manual" para casos esporádicos.
    *   **Harvest & Outros:**
        *   Listar configurações pré-definidas ou "Padrão".
        *   Executar diretamente após a seleção.

### 3. Limpeza de Código
*   Remover classes de configuração duplicadas que existam apenas no CLI (ex: `CliNotesSettings`).
*   Remover lógicas de persistência de arquivo JSON espalhadas pelos comandos individuais.

## Benefícios
*   **Produtividade:** Elimina digitação repetitiva no console.
*   **Consistência:** O que é configurado na interface visual reflete imediatamente no terminal.
*   **Manutenibilidade:** Uma única fonte de verdade para leitura/escrita de configurações.
