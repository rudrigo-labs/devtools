# Manual do Usuário e Documentação Técnica - DevTools

Bem-vindo ao **DevTools**, uma suíte de ferramentas de desenvolvimento projetada para aumentar a produtividade, automatizar tarefas repetitivas e fornecer utilitários essenciais para o dia a dia de engenharia de software.

## 1. Visão Geral

O projeto **DevTools** é composto por uma coleção modular de ferramentas (Tools), acessíveis através de duas interfaces principais:
1.  **CLI (Command Line Interface)**: Para automação, scripts e uso rápido no terminal.
2.  **WPF (Windows Presentation Foundation)**: Uma interface gráfica moderna para Windows, com suporte a bandeja do sistema (Tray Icon) e formulários interativos.

### Arquitetura
O sistema segue uma arquitetura limpa e modular:
-   **Core**: Contém abstrações, modelos comuns e utilitários compartilhados.
-   **Tools**: Cada ferramenta é um projeto independente (ex: `DevTools.Notes`, `DevTools.Ngrok`), garantindo isolamento e facilidade de manutenção.
-   **Presentation**: As camadas de apresentação (CLI e WPF) consomem as ferramentas através de interfaces unificadas (`IDevToolEngine`).

---

## 2. Instalação e Configuração

### Pré-requisitos
-   Windows 10/11
-   .NET SDK (versão mais recente, compatível com o projeto)

### Compilação
Para compilar todo o projeto, execute na raiz:

```powershell
dotnet build
```

### Executando a CLI
A ferramenta de linha de comando pode ser executada diretamente após a compilação:

```powershell
# Exemplo de execução
.\src\Cli\DevTools.Cli\bin\Debug\net10.0\DevTools.Cli.exe [comando]
```

### Executando a Interface Gráfica (WPF)
O aplicativo WPF pode ser iniciado e ficará residente na bandeja do sistema:

```powershell
.\src\Presentation\DevTools.Presentation.Wpf\bin\Debug\net10.0\DevTools.Presentation.Wpf.exe
```

---

## 3. Ferramentas Disponíveis

Abaixo está a documentação detalhada de cada ferramenta incluída na suíte.

### 3.1. Notes (Notas Locais)
Gerenciador de notas rápido e 100% local. Focado em privacidade e simplicidade.
-   **Funcionalidades**:
    -   Criar, ler e listar notas em Markdown ou Texto.
    -   Armazenamento em sistema de arquivos local (JSON index + arquivos).
    -   **Backup**: Exportação e Importação via arquivos ZIP.
    -   **Segurança**: Sem dependências de nuvem ou OAuth.
-   **Uso Típico**: Anotações rápidas durante reuniões, snippets de código, logs diários.

### 3.2. Harvest (Colheita de Código)
Ferramenta para coletar e consolidar arquivos de código fonte de um diretório para análise ou backup.
-   **Funcionalidades**:
    -   Escaneia um diretório raiz.
    -   Filtra arquivos baseados em regras (extensões, padrões).
    -   Copia arquivos relevantes para uma pasta de saída, mantendo ou achatando a estrutura.
    -   Gera relatórios de "colheita".

### 3.3. Organizer (Organizador de Arquivos)
Automatiza a organização de arquivos em uma pasta de entrada (Inbox).
-   **Funcionalidades**:
    -   Analisa arquivos em uma pasta de entrada.
    -   Aplica regras de classificação (por extensão, data, conteúdo ou palavras-chave).
    -   Move arquivos para pastas de destino organizadas.
    -   Suporte a modo "Simulação" (Dry Run) antes de aplicar as mudanças.

### 3.4. Rename (Renomeação em Massa)
Utilitário avançado para renomear arquivos e diretórios.
-   **Funcionalidades**:
    -   Renomeação baseada em padrões (Regex).
    -   Substituição de texto simples.
    -   **Roslyn Support**: Capacidade de refatoração de símbolos em código C# (se implementado/ativado).
    -   Preview de alterações antes da execução.

### 3.5. SearchText (Busca Textual)
Ferramenta de busca de texto em arquivos (similar ao Grep, mas otimizado para o fluxo do DevTools).
-   **Funcionalidades**:
    -   Busca recursiva em diretórios.
    -   Suporte a Expressões Regulares (Regex).
    -   Filtros de inclusão/exclusão de arquivos.
    -   Relatório detalhado de ocorrências (arquivo, linha, trecho).

### 3.6. Snapshot (Snapshot de Diretório)
Cria uma "foto" da estrutura de arquivos e pastas.
-   **Funcionalidades**:
    -   Gera uma representação da árvore de diretórios.
    -   Formatos de saída: JSON (estruturado), HTML (visualizável), Texto (árvore ASCII).
    -   Útil para documentar estrutura de projetos ou comparar estados de diretórios.

### 3.7. Utf8Convert (Conversor de Encoding)
Garante que arquivos de texto estejam na codificação UTF-8.
-   **Funcionalidades**:
    -   Detecta a codificação atual de arquivos.
    -   Converte para UTF-8 (com ou sem BOM, configurável).
    -   Processamento em lote (batch) de diretórios inteiros.

### 3.8. Image (Processamento de Imagem)
Utilitários para manipulação básica de imagens.
-   **Funcionalidades**:
    -   **Split**: Divide imagens grandes em partes menores (fatiamento/tiling).
    -   Útil para processamento de datasets ou otimização de assets web.

### 3.9. SSHTunnel (Túneis SSH)
Gerenciador de túneis SSH para redirecionamento de portas.
-   **Funcionalidades**:
    -   Criação e gerenciamento de processos SSH para port forwarding (Local/Remote).
    -   Monitoramento de status da conexão.
    -   Reconexão automática (dependendo da configuração).

### 3.10. Ngrok (Gerenciador Ngrok)
Wrapper para o utilitário Ngrok, facilitando a exposição de portas locais.
-   **Funcionalidades**:
    -   Inicia túneis HTTP/TCP via Ngrok.
    -   Gerencia processos do Ngrok.
    -   Visualização de URLs públicas geradas.

### 3.11. Migrations (EF Core Helper)
Auxiliar para execução de comandos do Entity Framework Core.
-   **Funcionalidades**:
    -   Facilita a geração de comandos `dotnet ef`.
    -   Gerenciamento de migrações de banco de dados.

---

## 4. Guia de Uso (Exemplos)

### Exemplo 1: Criando uma nota rápida (CLI)
```powershell
DevTools.Cli.exe notes
# Selecione a opção "3) Criar nota"
# Digite o título e o conteúdo
```

### Exemplo 2: Convertendo arquivos para UTF-8 (CLI)
```powershell
DevTools.Cli.exe utf8convert --path "C:\MeusProjetos\LegacyApp" --pattern "*.cs"
```

### Exemplo 3: Usando a Interface Gráfica
1.  Abra o `DevTools.Presentation.Wpf.exe`.
2.  O ícone aparecerá na bandeja do sistema (perto do relógio).
3.  Clique com o botão direito para ver o menu rápido ou clique duas vezes para abrir o Dashboard.
4.  No Dashboard, selecione a ferramenta desejada (ex: **Notes**, **Organizer**) para abrir sua janela de configuração e execução.

---

## 5. Estrutura de Diretórios (Desenvolvimento)

Para desenvolvedores que desejam estender o projeto:

-   `src/Cli`: Ponto de entrada da aplicação Console.
-   `src/Core`: Bibliotecas base.
-   `src/Presentation`: Aplicação WPF.
-   `src/Tools`:
    -   Cada pasta aqui é uma ferramenta autônoma.
    -   Para adicionar uma nova ferramenta, crie um novo projeto Class Library em `src/Tools`, implemente `IDevToolEngine` e registre-o no CLI e WPF.

---

*Documentação gerada automaticamente em 12/02/2026.*
