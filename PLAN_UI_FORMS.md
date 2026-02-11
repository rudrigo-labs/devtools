# Plano de Interface (UI) - Formulários das Ferramentas DevTools

Este documento detalha os requisitos de entrada (variáveis) e a proposta de layout para os formulários WPF de cada ferramenta, garantindo consistência visual e funcional.

## Padrão Visual Sugerido
- **Estilo:** Janelas flutuantes ("Cards"), sem borda nativa, sombra suave, tema Dark.
- **Estrutura:** Header (Título + Fechar), Body (Inputs agrupados), Footer (Ações: Cancelar/Executar).
- **Inputs:** `ModernTextBox`, `ModernButton` (Ação), `PathSelector` (Componente reutilizável para pastas/arquivos).

---

## 1. Harvest (Análise de Projeto)
*   **Variáveis:**
    *   `RootPath` (Pasta obrigatória - Origem)
    *   `OutputPath` (Pasta obrigatória - Destino das Cópias)
    *   `ConfigPath` (Arquivo JSON opcional - Hidden/Advanced)
*   **Comportamento:**
    *   Analisa os arquivos baseados em regras (extensões, patterns).
    *   **COPIA** todos os arquivos úteis encontrados para a pasta `OutputPath`.
    *   **Validação:** O usuário É OBRIGADO a escolher a pasta de destino (sem default).
    *   Fluxo assíncrono: Notifica na Tray ao terminar.
*   **Layout:**
    *   Card simples.
    *   Input: Source Folder (PathSelector).
    *   Input: Output Folder (PathSelector).
    *   Button: "Run Harvest".

## 2. Image (Splitter)
*   **Variáveis:**
    *   `InputPath` (Arquivo Imagem obrigatório)
    *   `OutputDirectory` (Pasta opcional)
    *   `OutputBaseName` (Texto)
    *   `AlphaThreshold`, `StartIndex`, `MinWidth`, `MinHeight` (Numéricos)
    *   `Overwrite` (Checkbox)
*   **Layout Sugerido:**
    *   Grupo "Source": Seletor de Arquivo de Imagem.
    *   Grupo "Target": Seletor de Pasta de Saída + Prefixo do Arquivo.
    *   Grupo "Settings" (Grid 2x2): Threshold, StartIndex, MinDimensions.
    *   Footer: Checkbox "Overwrite" ao lado do botão Run.

## 3. Migrations (EF Core Helper)
*   **Variáveis:**
    *   `Action` (Enum: AddMigration, UpdateDatabase)
    *   `Provider` (Enum: SqlServer, Postgres, Sqlite)
    *   `RootPath` (Pasta da Solução/Projeto)
    *   `StartupProjectPath` (Pasta do Projeto Startup)
    *   `DbContextFullName` (Texto - Classe do Contexto)
    *   `MigrationName` (Texto - Apenas para Add)
    *   `DryRun` (Checkbox)
*   **Layout Sugerido:**
    *   **Alta Complexidade**.
    *   Topo: Radio Buttons grandes para escolher Ação (Add vs Update).
    *   Corpo:
        *   Combobox "Database Provider".
        *   Inputs de Caminho (Root, Startup).
        *   Input de Texto "DbContext Class".
        *   Input de Texto "Migration Name" (Visível apenas se Ação == Add).
    *   Dica: Salvar as últimas configurações (Paths/DbContext) no UserSettings para não digitar sempre.

## 4. Ngrok (Tunnel Manager)
*   **Variáveis:**
    *   `Action` (Start, Stop)
    *   `Protocol` (Http, Tcp)
    *   `Port` (Inteiro)
    *   `TunnelName` (Texto opcional)
*   **Layout Sugerido:**
    *   Switch simples ou Toggle Button: "Start / Stop".
    *   Se Start: Mostrar campos Protocol (Combo), Port (Number), Name.
    *   Se Stop: Mostrar lista de túneis ativos para encerrar (se possível listar) ou botão "Stop All".

## 5. Notes (Quick Notes)
*   **Variáveis:**
    *   `Content` (Texto)
    *   `Action` (Read/Write)
*   **Layout Sugerido:**
    *   **Diferenciado:** Não deve ser um formulário de "Executar", mas sim a própria ferramenta de notas.
    *   Sugestão: Janela que já abre com TextArea grande para digitar e salvar automaticamente ou enviar.

## 6. Organizer (File Organizer) - **JÁ IMPLEMENTADO**
*   **Variáveis:**
    *   `InputPath` (Pasta)
    *   `OutputPath` (Pasta)
    *   `Simulate` (Checkbox)
*   **Layout:**
    *   Dois seletores de pasta verticais.
    *   Checkbox de simulação.

## 7. Rename (Bulk Renamer)
*   **Variáveis:**
    *   `RootPath` (Pasta)
    *   `OldText`, `NewText` (Strings)
    *   `Mode` (Enum: General, Class, File)
    *   `Globs` (Include/Exclude patterns)
    *   `DryRun`, `Backup` (Checkboxes)
*   **Layout Sugerido:**
    *   Topo: Seletor de Pasta.
    *   Centro: Grid "De -> Para".
        *   Esquerda: `OldText`, Direita: `NewText`.
    *   Abaixo: Combobox "Mode" (Renomear Arquivo vs Conteúdo vs Classe).
    *   Expander "Filters": Inputs para Globs.

## 8. SSHTunnel (Port Forwarding)
*   **Variáveis:**
    *   `Profile` (Host, Port, User, KeyPath, LocalPort, RemoteHost, RemotePort)
*   **Layout Sugerido:**
    *   **Gerenciador de Perfis**.
    *   Combobox "Selecionar Perfil" (Default, Prod, Staging).
    *   Botão "Edit Profiles" (abre modal detalhado com todos os campos técnicos).
    *   Botão Principal: "Connect" (vira "Disconnect" se rodando).

## 9. SearchText (Grep-like)
*   **Variáveis:**
    *   `RootPath` (Pasta)
    *   `Pattern` (Texto)
    *   `UseRegex`, `CaseSensitive` (Checkboxes)
*   **Layout Sugerido:**
    *   Similar ao VS Code Search.
    *   Input "Search Pattern" com toggles pequenos dentro (.*, Aa, \b).
    *   Seletor de Pasta.
    *   Botão "Search" -> Abre resultado em nova janela ou output visual? (Para Tray, talvez gerar arquivo de report ou abrir notepad).

## 10. Snapshot (Project Backup/Export)
*   **Variáveis:**
    *   `RootPath` (Pasta)
    *   `OutputBasePath` (Pasta)
    *   `Formats` (Checkboxes: Text, Json, Html)
    *   `IgnoredDirectories` (Lista)
*   **Layout Sugerido:**
    *   Seletor de Pasta "Source".
    *   Seletor de Pasta "Destination".
    *   Grupo "Formats": 3 Checkboxes lado a lado.

## 11. Utf8Convert (Encoding Fixer)
*   **Variáveis:**
    *   `RootPath` (Pasta)
    *   `Recursive`, `BOM`, `Backup` (Checkboxes)
*   **Layout Sugerido:**
    *   Seletor de Pasta.
    *   Lista vertical de opções booleanas (Checkboxes).

---

## Próximos Passos
1.  Aprovar este plano.
2.  Criar componente reutilizável `PathSelector` (Label + Input + Button).
3.  Implementar formulários em ordem de prioridade/complexidade.
