# Plano de Interface (UI) - FormulÃ¡rios das Ferramentas DevTools

Este documento detalha os requisitos de entrada (variÃ¡veis) e a proposta de layout para os formulÃ¡rios WPF de cada ferramenta, garantindo consistÃªncia visual e funcional.

## PadrÃ£o Visual Sugerido
- **Estilo:** Janelas flutuantes ("Cards"), sem borda nativa, sombra suave, tema Dark.
- **Estrutura:** Header (TÃ­tulo + Fechar), Body (Inputs agrupados), Footer (AÃ§Ãµes: Cancelar/Executar).
- **Inputs:** `ModernTextBox`, `ModernButton` (AÃ§Ã£o), `PathSelector` (Componente reutilizÃ¡vel para pastas/arquivos).

---

## 1. Harvest (AnÃ¡lise de Projeto)
*   **VariÃ¡veis:**
    *   `RootPath` (Pasta obrigatÃ³ria - Origem)
    *   `OutputPath` (Pasta obrigatÃ³ria - Destino das CÃ³pias)
    *   `ConfigPath` (Arquivo JSON opcional - Hidden/Advanced)
*   **Comportamento:**
    *   Analisa os arquivos baseados em regras (extensÃµes, patterns).
    *   **COPIA** todos os arquivos Ãºteis encontrados para a pasta `OutputPath`.
    *   **ValidaÃ§Ã£o:** O usuÃ¡rio Ã‰ OBRIGADO a escolher a pasta de destino (sem default).
    *   Fluxo assÃ­ncrono: Notifica na Tray ao terminar.
*   **Layout:**
    *   Card simples.
    *   Input: Source Folder (PathSelector).
    *   Input: Output Folder (PathSelector).
    *   Button: "Run Harvest".

## 2. Image (Splitter)
*   **VariÃ¡veis:**
    *   `InputPath` (Arquivo Imagem obrigatÃ³rio)
    *   `OutputDirectory` (Pasta opcional)
    *   `OutputBaseName` (Texto)
    *   `AlphaThreshold`, `StartIndex`, `MinWidth`, `MinHeight` (NumÃ©ricos)
    *   `Overwrite` (Checkbox)
*   **Layout Sugerido:**
    *   Grupo "Source": Seletor de Arquivo de Imagem.
    *   Grupo "Target": Seletor de Pasta de SaÃ­da + Prefixo do Arquivo.
    *   Grupo "Settings" (Grid 2x2): Threshold, StartIndex, MinDimensions.
    *   Footer: Checkbox "Overwrite" ao lado do botÃ£o Run.

## 3. Migrations (EF Core Helper)
*   **VariÃ¡veis:**
    *   `Action` (Enum: AddMigration, UpdateDatabase)
    *   `Provider` (Enum: SqlServer, Postgres, Sqlite)
    *   `RootPath` (Pasta da SoluÃ§Ã£o/Projeto)
    *   `StartupProjectPath` (Pasta do Projeto Startup)
    *   `DbContextFullName` (Texto - Classe do Contexto)
    *   `MigrationName` (Texto - Apenas para Add)
    *   `DryRun` (Checkbox)
*   **Layout Sugerido:**
    *   **Alta Complexidade**.
    *   Topo: Radio Buttons grandes para escolher AÃ§Ã£o (Add vs Update).
    *   Corpo:
        *   Combobox "Database Provider".
        *   Inputs de Caminho (Root, Startup).
        *   Input de Texto "DbContext Class".
        *   Input de Texto "Migration Name" (VisÃ­vel apenas se AÃ§Ã£o == Add).
    *   Dica: Salvar as Ãºltimas configuraÃ§Ãµes (Paths/DbContext) no UserSettings para nÃ£o digitar sempre.

## 4. Ngrok (Tunnel Manager)
*   **VariÃ¡veis:**
    *   `Action` (Start, Stop)
    *   `Protocol` (Http, Tcp)
    *   `Port` (Inteiro)
    *   `TunnelName` (Texto opcional)
*   **Layout Sugerido:**
    *   Switch simples ou Toggle Button: "Start / Stop".
    *   Se Start: Mostrar campos Protocol (Combo), Port (Number), Name.
    *   Se Stop: Mostrar lista de tÃºneis ativos para encerrar (se possÃ­vel listar) ou botÃ£o "Stop All".

## 5. Notes (Quick Notes)
*   **VariÃ¡veis:**
    *   `Content` (Texto)
    *   `Action` (Read/Write)
*   **Layout Sugerido:**
    *   **Diferenciado:** NÃ£o deve ser um formulÃ¡rio de "Executar", mas sim a prÃ³pria ferramenta de notas.
    *   SugestÃ£o: Janela que jÃ¡ abre com TextArea grande para digitar e salvar automaticamente ou enviar.

## 6. Organizer (File Organizer) - **JÃ IMPLEMENTADO**
*   **VariÃ¡veis:**
    *   `InputPath` (Pasta)
    *   `OutputPath` (Pasta)
    *   `Simulate` (Checkbox)
*   **Layout:**
    *   Dois seletores de pasta verticais.
    *   Checkbox de simulaÃ§Ã£o.

## 7. Rename (Bulk Renamer)
*   **VariÃ¡veis:**
    *   `RootPath` (Pasta)
    *   `OldText`, `NewText` (Strings)
    *   `Mode` (Enum: General, Class, File)
    *   `Globs` (Include/Exclude patterns)
    *   `DryRun`, `Backup` (Checkboxes)
*   **Layout Sugerido:**
    *   Topo: Seletor de Pasta.
    *   Centro: Grid "De -> Para".
        *   Esquerda: `OldText`, Direita: `NewText`.
    *   Abaixo: Combobox "Mode" (Renomear Arquivo vs ConteÃºdo vs Classe).
    *   Expander "Filters": Inputs para Globs.

## 8. SSHTunnel (Port Forwarding)
*   **VariÃ¡veis:**
    *   `Configuration` (Host, Port, User, KeyPath, LocalPort, RemoteHost, RemotePort)
*   **Layout Sugerido:**
    *   **Gerenciador de Configuracoes**.
    *   Combobox "Selecionar Configuracao" (Default, Prod, Staging).
    *   BotÃ£o "Edit Configurations" (abre modal detalhado com todos os campos tÃ©cnicos).
    *   BotÃ£o Principal: "Connect" (vira "Disconnect" se rodando).

## 9. SearchText (Grep-like)
*   **VariÃ¡veis:**
    *   `RootPath` (Pasta)
    *   `Pattern` (Texto)
    *   `UseRegex`, `CaseSensitive` (Checkboxes)
*   **Layout Sugerido:**
    *   Similar ao VS Code Search.
    *   Input "Search Pattern" com toggles pequenos dentro (.*, Aa, \b).
    *   Seletor de Pasta.
    *   BotÃ£o "Search" -> Abre resultado em nova janela ou output visual? (Para Tray, talvez gerar arquivo de report ou abrir notepad).

## 10. Snapshot (Project Backup/Export)
*   **VariÃ¡veis:**
    *   `RootPath` (Pasta)
    *   `OutputBasePath` (Pasta)
    *   `Formats` (Checkboxes: Text, Json, Html)
    *   `IgnoredDirectories` (Lista)
*   **Layout Sugerido:**
    *   Seletor de Pasta "Source".
    *   Seletor de Pasta "Destination".
    *   Grupo "Formats": 3 Checkboxes lado a lado.

## 11. Utf8Convert (Encoding Fixer)
*   **VariÃ¡veis:**
    *   `RootPath` (Pasta)
    *   `Recursive`, `BOM`, `Backup` (Checkboxes)
*   **Layout Sugerido:**
    *   Seletor de Pasta.
    *   Lista vertical de opÃ§Ãµes booleanas (Checkboxes).

---

## PrÃ³ximos Passos
1.  Aprovar este plano.
2.  Criar componente reutilizÃ¡vel `PathSelector` (Label + Input + Button).
3.  Implementar formulÃ¡rios em ordem de prioridade/complexidade.

