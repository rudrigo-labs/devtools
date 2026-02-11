# 📖 Manual do Usuário - DevTools

Este documento detalha o funcionamento de cada ferramenta do pacote **DevTools**, bem como os arquivos de configuração utilizados pelo sistema.

---

## 🛠️ Ferramentas Detalhadas

### 1. 📝 Notes (Notas Rápidas)
Um bloco de notas persistente e flutuante.
*   **Uso:** Ideal para colar snippets, TODOs rápidos ou anotações de reuniões.
*   **Armazenamento:**
    *   Padrão: `%APPDATA%\DevTools\QuickNotes.txt`
    *   Integração: Pode ser configurado para salvar no **OneDrive** ou **Google Drive** (via menu de configurações na própria janela).
*   **Comportamento:** Salva automaticamente ao perder o foco ou fechar.

### 2. 🌾 Harvest (Code Harvester)
Coleta código de múltiplos arquivos em um único arquivo de texto.
*   **Entrada:** Diretório raiz do projeto.
*   **Saída:** Arquivo `.txt` contendo o caminho e o conteúdo de cada arquivo encontrado.
*   **Filtros:** Ignora automaticamente pastas `bin`, `obj`, `.git`, `node_modules`.
*   **Caso de Uso:** Gerar contexto para enviar para IAs (ChatGPT, Claude) analisarem um projeto inteiro.

### 3. ✂️ ImageSplitter
Divide uma imagem grande em várias partes menores.
*   **Entrada:** Arquivo de imagem (PNG, JPG).
*   **Configuração:** Define o tamanho do recorte ou número de linhas/colunas.
*   **Saída:** Pasta com as imagens recortadas sequencialmente.

### 4. 🗄️ Migrations (EF Core Helper)
Interface gráfica para comandos do `dotnet ef`.
*   **Requisitos:** O projeto deve usar Entity Framework Core e ter o `dotnet-ef` instalado globalmente.
*   **Funcionalidades:**
    *   `Add Migration`: Cria uma nova migração com o nome especificado.
    *   `Update Database`: Aplica as migrações pendentes ao banco de dados.
*   **Parâmetros:** Seleção do Projeto de Inicialização (Startup Project) e Projeto de Migrações.

### 5. 🌐 Ngrok Manager
Gerencia túneis públicos para seu localhost.
*   **Requisitos:** Executável do `ngrok` no PATH ou configurado.
*   **Funcionalidades:**
    *   Listar túneis ativos.
    *   Iniciar novo túnel HTTP em porta específica.
    *   Matar todos os túneis.

### 6. 🏷️ Rename (Bulk Rename)
Renomeação em massa de arquivos e pastas.
*   **Modos:**
    *   **Simples:** Substituição de texto simples.
    *   **Regex:** Substituição avançada usando Expressões Regulares.
*   **Segurança:** Opção de backup antes de renomear e log de desfazer (Undo).

### 7. 📸 Snapshot
Gera um relatório estático da estrutura e conteúdo do projeto.
*   **Formatos:**
    *   **JSON:** Estrutura hierárquica para análise via script.
    *   **HTML:** Visualização navegável em árvore.
    *   **Texto:** Árvore de diretórios simples (similar ao comando `tree`).

### 8. 🔒 SSH Tunnel
Gerenciador de túneis SSH (Port Forwarding).
*   **Perfis:** Permite salvar perfis de conexão (Host, Porta, Usuário, Chave Privada, Portas Local/Remota).
*   **Uso:** Cria um túnel local que redireciona para um serviço em um servidor remoto (ex: acessar um banco de dados de produção via `localhost:5432`).

### 9. 🔣 Utf8Convert
Converte a codificação de arquivos de texto.
*   **Problema:** Corrige arquivos com caracteres estranhos (encoding ANSI/Windows-1252) para UTF-8 universal.
*   **Opções:** Adicionar ou remover BOM (Byte Order Mark).

### 10. 🪵 Logs do Sistema
Visualizador interno de logs para diagnóstico e monitoramento.
*   **Acesso:** Disponível no menu de contexto da bandeja (Tray) -> "Logs do Sistema".
*   **Funcionalidades:**
    *   Exibe logs detalhados de erros (incluindo Stack Trace e Inner Exceptions).
    *   Monitora falhas em Jobs, Tarefas em Background e UI.
    *   **Botões:** Atualizar (reler arquivo), Limpar (apagar conteúdo) e Abrir Pasta (navegar no Explorer).
*   **Localização:** `%APPDATA%\DevTools\logs\DevTools.Presentation.Wpf.log`

---

## ⚙️ Arquivos de Configuração

O DevTools armazena suas configurações na pasta de dados do usuário:
📂 `%APPDATA%\DevTools` (Ex: `C:\Users\SeuUsuario\AppData\Roaming\DevTools`)

### `settings.json`
Arquivo principal de configuração do Tray App. Armazena:
*   **Posições das Janelas:** Coordenadas `Top`/`Left` para que as janelas reabram no mesmo lugar (gerenciado pelo sistema para ficar no canto inferior direito).
*   **Últimos Caminhos:** Diretórios usados recentemente em cada ferramenta (Harvest, Rename, etc.) para agilizar o reuso.
*   **Preferências:** Caminho do arquivo de notas, tema (se aplicável).

### 📂 Pasta `logs/`
Armazena os arquivos de log gerados pela aplicação (`DevTools.Presentation.Wpf.log`). Útil para auditoria e depuração de erros que não aparecem na interface.

**Exemplo:**
```json
{
  "LastHarvestSourcePath": "C:\\Projetos\\MeuApp",
  "NotesStoragePath": "C:\\Users\\Rodrigo\\OneDrive\\DevNotes.txt",
  "Utf8WindowTop": 800,
  "Utf8WindowLeft": 1200
}
```

### `ssh_profiles.json` (ou similar dentro da pasta de dados)
Armazena os perfis de conexão SSH criados na ferramenta **SSH Tunnel**.
*   **Conteúdo:** Lista de objetos com Host, User, KeyPath, PortMappings.
*   **Segurança:** As senhas **não** são salvas (uso recomendado de Chaves SSH).

### `ngrok_config.yml` (Opcional)
O DevTools usa a configuração padrão do Ngrok instalada no sistema, mas pode respeitar arquivos YAML locais se especificado na execução do processo.

---

## 💡 Dicas de Uso

*   **Atalhos:** Use o `Alt+Tab` para alternar rapidamente entre sua IDE e a ferramenta aberta (Notes, por exemplo).
*   **Limite de Janelas:** O sistema permite apenas **uma ferramenta aberta por vez** (além do Dashboard), mantendo sua área de trabalho limpa. Ao abrir uma nova ferramenta, a anterior é fechada automaticamente.
*   **Tray Icon:** O ícone na bandeja muda de cor ou exibe notificações (balões) quando tarefas longas (como um Harvest grande) são concluídas.
