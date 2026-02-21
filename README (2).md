# 🚀 DevTools

> **Sua suíte de produtividade para engenharia de software.**
> Um ecossistema modular de ferramentas .NET projetado para automatizar tarefas, organizar arquivos e acelerar o desenvolvimento.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Platform](https://img.shields.io/badge/platform-windows-lightgrey.svg)
![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)

---

## 📋 Visão Geral

O **DevTools** é uma coleção de utilitários essenciais para desenvolvedores, acessíveis via **Linha de Comando (CLI)** ou **Interface Gráfica (WPF/Tray)**.

O projeto segue uma arquitetura limpa onde cada ferramenta é uma biblioteca isolada (*Engine*), garantindo que a lógica de negócio seja desacoplada da apresentação.

### ✨ Principais Funcionalidades

| Ferramenta | Descrição |
| :--- | :--- |
| **📝 Notes** | Gerenciador de notas rápido, 100% local (Markdown), com backup ZIP e foco em privacidade. |
| **🌾 Harvest** | Coletor de código-fonte para análise ou backup, com filtros inteligentes. |
| **📂 Organizer** | Organiza arquivos em pastas automaticamente baseado em regras (extensão, data, etc). |
| **🏷️ Rename** | Renomeação em massa avançada com suporte a Regex e preview. |
| **🔍 SearchText** | Busca textual rápida em diretórios (Grep-like) otimizada para dev. |
| **📸 Snapshot** | Gera "fotos" da estrutura de diretórios em JSON, HTML ou Árvore de Texto. |
| **🔣 Utf8Convert** | Detecta e converte codificação de arquivos para UTF-8 em lote. |
| **🖼️ Image** | Utilitários de imagem, incluindo fatiamento (split) para datasets/web. |
| **🔒 SSHTunnel** | Gerenciador de túneis SSH para port forwarding local/remoto. |
| **🌐 Ngrok** | Wrapper para gerenciamento fácil de túneis HTTP/TCP via Ngrok. |
| **🗄️ Migrations** | Auxiliar para comandos do Entity Framework Core. |

---

## 🚀 Como Usar

### Pré-requisitos
*   Windows 10 ou 11
*   .NET SDK 10.0+

### Instalação e Build

Clone o repositório e compile o projeto:

```powershell
git clone https://github.com/seu-usuario/devtools.git
cd devtools
dotnet build
```

### 🖥️ Interface Gráfica (WPF)
A maneira mais fácil de usar. O aplicativo fica na bandeja do sistema (Tray Icon).

Execute:
`.\src\Presentation\DevTools.Presentation.Wpf\bin\Debug\net10.0\DevTools.Presentation.Wpf.exe`

*   **Clique duplo** no ícone da bandeja para abrir o Dashboard.
*   **Clique direito** para acesso rápido às ferramentas.

### ⌨️ Linha de Comando (CLI)
Para automação e scripts.

Execute:
`.\src\Cli\DevTools.Cli\bin\Debug\net10.0\DevTools.Cli.exe [comando]`

Exemplos:
```powershell
# Criar uma nota
devcli notes

# Converter arquivos para UTF-8
devcli utf8convert --path "C:\Projetos\Legacy" --pattern "*.cs"
```

---

## 🏗️ Arquitetura

O projeto é estruturado em camadas para máxima reutilização:

1.  **Core (`DevTools.Core`)**: Contratos, interfaces e utilitários base.
2.  **Tools (`DevTools.*`)**: Bibliotecas independentes contendo a lógica de cada ferramenta (Engines).
3.  **Presentation**:
    *   **CLI (`DevTools.Cli`)**: Interface de terminal.
    *   **WPF (`DevTools.Presentation.Wpf`)**: Interface gráfica moderna.

---

## 📚 Documentação

Para detalhes completos de cada ferramenta, consulte o [Manual do Usuário](MANUAL.md).

---

## 📄 Licença

Este projeto está licenciado sob a licença MIT - veja o arquivo [LICENSE](LICENSE) para detalhes.

---

<p align="center">
  Desenvolvido com ❤️ por <b>Rudrigo Labs</b>
</p>
