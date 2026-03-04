# DevTools - Guia do Usuário

O **DevTools** é um conjunto de utilitários focados em produtividade, acessíveis rapidamente via bandeja do sistema (System Tray) ou através de um Dashboard centralizado.

## Acesso e Navegação

### Bandeja do Sistema (Tray)
Ao iniciar, o DevTools fica residente na bandeja do sistema (próximo ao relógio do Windows).
- **Clique Duplo:** Abre o **Dashboard (Painel Principal)**.
- **Clique Direito:** Exibe o menu de contexto com acesso rápido a todas as ferramentas individuais.

### Dashboard (Hub)
O painel centraliza todas as funcionalidades:
- **Aba Ferramentas:** Lista de cards para abrir cada utilitário.
- **Aba Execuções (Jobs):** Monitoramento em tempo real de tarefas em background (como Renomeação ou Migrations), exibindo barra de progresso e logs detalhados.
- **Aba Configurações:** (Em breve) Ajustes globais da aplicação.

---

## Ferramentas Disponíveis

### 1. Notas Rápidas (Notes)
Bloco de notas flutuante "always-on-top" (sempre visível).
- **Uso:** Ideal para copiar/colar temporário, rascunhos rápidos ou anotações durante reuniões.
- **Sincronização Cloud:**
  - Suporte nativo a **Google Drive** e **OneDrive** via API (sem necessidade de instalar os aplicativos de desktop).
  - Autenticação segura via OAuth (browser do sistema).
  - Sincronização manual via botão na barra de título (ícone de nuvem).
  - Resolução de conflitos (não sobrescreve dados, cria arquivos de conflito).
- **Armazenamento:**
  - O conteúdo é salvo automaticamente em arquivos locais (`.txt`/`.md`).
  - Permite escolher o diretório local de armazenamento via menu de contexto.

### 2. Organizador de Arquivos (Organizer)
Move arquivos de uma pasta de entrada para uma estrutura organizada.
- **Modos:**
  - Por Extensão (ex: `Imagens/`, `Documentos/`).
  - Por Data (ex: `2023/10/`).
- **Configuração:** Selecione a pasta de origem e o modo desejado.

### 3. Renomeador em Massa (Rename)
Renomeia múltiplos arquivos usando padrões (Regex).
- **Uso:** Defina a pasta, o padrão de busca (Regex) e o padrão de substituição.
- **Exemplo:** Trocar espaços por underlines ou adicionar prefixos.

### 4. Snapshot de Projeto
Cria um backup compactado (.zip) do diretório atual.
- **Filtros:** Ignora automaticamente pastas como `bin`, `obj`, `.git`, `node_modules`.
- **Uso:** Ótimo para criar "pontos de restauração" manuais antes de alterações arriscadas.

### 5. Divisor de Imagens (Image Splitter)
Divide uma imagem grande em pedaços menores (grid).
- **Uso:** Útil para preparar imagens para redes sociais (ex: mosaico do Instagram).

### 6. Conversor UTF-8
Corrige a codificação de arquivos de texto para UTF-8.
- **Uso:** Resolve problemas de acentuação em arquivos antigos ou baixados.

### 7. Busca de Texto (SearchText)
Ferramenta de "grep" visual.
- **Uso:** Busca por termos ou Regex dentro de arquivos de uma pasta.
- **Diferencial:** Rápido e permite abrir o arquivo na linha correspondente.

### 8. Migrations Helper
Interface gráfica para comandos do Entity Framework Core.
- **Funcionalidades:** `Add-Migration`, `Update-Database`.
- **Uso:** Evita decorar comandos CLI complexos. Selecione o projeto e execute.

### 9. Harvest (Scanner)
Copia arquivos de uma árvore de diretórios para uma pasta única, baseado em filtros.
- **Uso:** "Colher" todos os `.pdf` espalhados em subpastas para um único local.

### 10. Túnel SSH (Gerenciador de Conexões)
Gerencia túneis SSH persistentes para encaminhamento de portas (Port Forwarding), permitindo acessar serviços remotos (como bancos de dados) como se estivessem locais.

**Funcionalidades Principais:**
- **Lista de Perfis:**
  - Localizada na barra lateral esquerda, permite gerenciar múltiplas conexões.
  - **Adicionar (+):** Cria um novo perfil para configuração.
  - **Remover (-):** Exclui o perfil selecionado permanentemente.
- **Formulário de Configuração:**
  - **Nome:** Identificador amigável para o perfil na lista.
  - **Host/Porta SSH:** Endereço do servidor de salto (Bastion/Jump Box) e porta (padrão 22).
  - **Autenticação:** Suporta usuário e caminho para **Chave Privada** (arquivos `.pem`, `.ppk`, `.key`).
  - **Encaminhamento (Port Forwarding):**
    - **Local:** Endereço (`127.0.0.1`) e porta onde o serviço ficará acessível no seu PC.
    - **Remoto:** Endereço e porta do serviço de destino (ex: banco de dados) na rede remota.
- **Controles:**
  - **Conectar/Desconectar:** Botão principal com indicador de status visual (Verde/Cinza).
  - **Minimizar para Bandeja:** Botão dedicado na barra lateral para manter o túnel rodando em segundo plano sem ocupar espaço na barra de tarefas.
- **Persistência:** Todos os perfis são salvos automaticamente no arquivo central `appsettings.json`.

---

## Configurações e Persistência

- **AppSettings:** As configurações (últimos caminhos usados, posições de janela) são salvas automaticamente em `AppSettings.json` no diretório da aplicação.
- **Logs:** Erros críticos são exibidos em janelas de alerta ou logs do sistema.
