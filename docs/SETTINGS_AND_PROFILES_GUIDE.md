# Guia de Configurações e Perfis (DevTools Hub)

Este documento explica como o DevTools Hub armazena e gerencia as configurações globais e os perfis de ferramentas. Se você esqueceu onde os dados ficam salvos ou como eles são estruturados, este guia é para você.

## 1. Onde os dados são salvos?

Existem três locais principais de armazenamento, todos em formato **JSON**:

### A. Configurações Globais das Ferramentas (`appsettings.json`)
- **Local**: Na mesma pasta onde o executável (`.exe`) está rodando.
- **Gerenciado por**: `ConfigService.cs`.
- **Conteúdo**: Configurações que raramente mudam e são compartilhadas por toda a ferramenta.
  - **Harvest**: Pesos de IA (Fan-In/Out), extensões permitidas e pastas ignoradas por padrão.
  - **Migrations**: Caminho raiz do projeto, projeto de startup e nome do DbContext.
  - **Ngrok**: Token de autenticação e argumentos extras de linha de comando.
  - **SSH (Legado)**: Antigos perfis que estamos migrando para o novo sistema.

### B. Configurações do Aplicativo (`settings.json`)
- **Local**: `%AppData%\DevTools\settings.json`.
- **Gerenciado por**: `SettingsService.cs`.
- **Conteúdo**: Preferências de uso da interface e dados voláteis.
  - **Notas Rápidas**: O conteúdo das suas notas.
  - **Posição de Janelas**: Salva onde você deixou as janelas pela última vez.
  - **Temas**: Preferências de visual (se aplicável).

### C. Perfis de Ferramentas (`devtools.[ferramenta].json`)
- **Local**: `%AppData%\DevTools\profiles\`.
- **Gerenciado por**: `ProfileManager.cs`.
- **Conteúdo**: Cada ferramenta tem seu próprio arquivo JSON (ex: `devtools.sshtunnel.json`).
  - **Estrutura**: Uma lista de perfis, onde cada perfil tem um nome, um indicador se é o padrão (`IsDefault`) e um dicionário de opções (Chave/Valor).
  - **Ferramentas que usam perfis**: Túnel SSH, Rename, SearchText, Snapshot, Migrations e Harvest.

---

## 2. Como as ferramentas usam esses dados?

### Exemplo: Coletor de Arquivos (Harvest)
O Harvest usa um sistema híbrido:
1. Ele lê as **regras padrão** (extensões, pastas ignoradas) do `appsettings.json`.
2. Ele permite que você crie **perfis** (ex: "Projeto Web", "Projeto Mobile") que ficam salvos em `%AppData%\DevTools\profiles\devtools.harvest.json`.
3. Na janela da ferramenta, ele carrega o perfil marcado como **padrão** automaticamente.

### Exemplo: Túnel SSH
O SSH foi migrado 100% para o sistema de perfis:
- Todos os dados de conexão (Host, Porta, Usuário, Chave) ficam no arquivo de perfil.
- Não há mais configurações de SSH no `appsettings.json` (embora o código legado ainda possa ler de lá como fallback).

---

## 3. Manutenção e Backup
Se você precisar formatar o computador ou quiser levar suas configurações para outra máquina:
1. Copie o arquivo `appsettings.json` da pasta do programa.
2. Copie a pasta `%AppData%\DevTools` inteira.

## 4. Dica Técnica para Desenvolvedores
Para adicionar um novo campo a um perfil:
1. Adicione o campo na UI dinâmica em `ProfileUIService.cs`.
2. O sistema de salvamento é genérico e usará a `Tag` do componente como chave no JSON automaticamente.
