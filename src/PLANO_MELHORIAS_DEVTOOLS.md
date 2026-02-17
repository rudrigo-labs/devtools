## Plano de Melhorias – DevTools

Este documento organiza as principais melhorias sugeridas para o projeto DevTools, divididas em fases, com checklists práticos para execução.

---

## Fase 1 – Higiene e Segurança Básica

Objetivo: deixar o projeto saudável sem alterar comportamento funcional.

### 1.1 Atualização de pacotes vulneráveis

- [x] Mapear onde cada pacote é utilizado:
  - [x] `BouncyCastle.Cryptography`
  - [x] `SixLabors.ImageSharp`
  - [x] `System.Security.Cryptography.Pkcs`
- [x] Verificar changelogs e notas de versão dos pacotes.
- [x] Definir versões alvo que resolvam os CVEs (mínimo necessário).
- [x] Atualizar versões nos respectivos `.csproj`.
- [x] Rodar:
  - [x] `dotnet restore`
  - [x] `dotnet build` da solution completa.
- [x] Exercitar manualmente os fluxos que dependem desses pacotes:
  - [x] Funcionalidades que usam imagem (ImageSplit / Organizer, se aplicável).
  - [x] Funcionalidades que usam criptografia/certificados (se houver).

### 1.2 Limpeza de warnings de build (quando fizer sentido)

- [x] Listar warnings atuais de build e classificá-los:
  - [x] Warnings aceitáveis e conhecidos (por exemplo, `NU1510` em `System.Text.Encoding.CodePages` no `DevTools.Cli`).
  - [x] Warnings que valem correção imediata.
- [x] Corrigir warnings triviais:
  - [x] Usings não utilizados.
  - [x] APIs obsoletas com substituição direta.
- [x] (Opcional) Marcar com `NoWarn` apenas warnings conscientemente aceitos, se necessário.

---

## Fase 2 – Tratamento de Erros e Logging

Objetivo: padronizar tratamento de erros e uso de logs, sem reescrever telas.

### 2.1 Definir contrato de erro de UI

- [x] Definir como deve ser uma mensagem de erro padrão:
  - [x] Curta e compreensível para o usuário.
  - [x] Detalhes técnicos indo para log (não para o `MessageBox`).
- [x] Especificar uma função/utilitário central de erro de UI (conceito):
  - [x] Ex.: `UiError.ShowError(string title, string userMessage, Exception ex)` (na prática, `UiMessageService.ShowError`).
  - [x] Internamente:
    - [x] Logar detalhes via `AppLogger`.
    - [x] Mostrar mensagem amigável para o usuário (WPF).

### 2.2 Integrar helper de erro nas janelas principais

- [x] Identificar janelas mais críticas em termos de erro:
  - [x] `LogsWindow`
  - [x] `MigrationsWindow`
  - [x] `OrganizerWindow`
  - [x] `HarvestWindow`
  - [x] `SshTunnelWindow`
  - [x] Outras que façam operações de IO/processos.
- [x] Substituir `MessageBox.Show(ex.Message, ...)` por chamadas ao helper central:
  - [x] Passar título/contexto significativo.
  - [x] Garantir que detalhes fiquem nos logs.

### 2.3 Padronizar logs de jobs

- [x] Revisar uso de `JobManager`:
  - [x] Garantir log de início de cada job (nome, parâmetros principais).
  - [x] Garantir log de fim de cada job (sucesso/erro, resumo).
- [x] Definir convenção de categoria/prefixo de log por ferramenta:
  - [x] Ex.: `[Organizer]`, `[Harvest]`, `[Migrations]`, `[SSHTunnel]`, `[UI]`.
- [x] Ajustar mensagens de conclusão de job para um formato consistente:
  - [x] Sucesso: resumo ("X processados, Y alterados, Z ignorados").
  - [x] Erro: frase de alto nível + indicação para olhar a janela de logs.

---

## Fase 3 – Validação de Inputs e Configurações

Objetivo: tornar validações mais previsíveis e fáceis de ajustar no futuro.

### 3.1 Extrair validações por ferramenta

- [x] Para cada janela que dispara engines:
- [x] `HarvestWindow`
- [x] `OrganizerWindow`
- [x] `MigrationsWindow`
- [x] `RenameWindow`
- [x] Outras conforme necessário.
- [x] Criar métodos explícitos de validação, por exemplo:
- [x] `ValidationResult ValidateInputs()` ou equivalente.
- [x] Concentrar regras de validação nesses métodos:
- [x] Required de paths.
- [x] Formato de números (score, portas, etc.).
- [x] Campos obrigatórios para cada ação (ex.: nome de migration).

### 3.2 Integrar validação com UI e logging

- [x] Na ação principal de cada janela (ex.: `Run`, `Execute`, etc.):
- [x] Chamar método de validação.
- [x] Em caso de erro, usar helper de UI (`UiError` ou similar) ou mensagens padronizadas de atenção.
- [x] Garantir que erros de configuração (paths não encontrados, JSON inválido, etc.) gerem:
- [x] Mensagem amigável para o usuário.
- [x] Registro detalhado no log.

### 3.3 Revisar ConfigService e perfis

- [x] Analisar `ConfigService`:
  - [x] Definir claramente seções de configuração (SSH, Harvest, Organizer, etc.).
  - [x] Melhorar mensagens quando o arquivo não existir / estiver inválido.
- [x] Revisar estrutura de perfis por ferramenta:
  - [x] Identificar chaves realmente essenciais.
  - [x] Avaliar se há redundância ou campos pouco usados.

---

## Fase 4 – Robustez das Engines e Refinos de WPF

Objetivo: dar sustentação para refactors futuros e deixar a UI mais limpa.

### 4.1 Testes automatizados nas engines principais

- [x] Criar projeto(s) de teste para engines (se ainda não existir):
- [x] Ex.: `DevTools.Organizer.Tests`, `DevTools.Rename.Tests`, etc.
- [x] Cobrir cenários básicos:
- [x] `OrganizerEngine` com pastas de exemplo, incluindo:
- [x] Arquivos a serem movidos.
- [x] Arquivos ignorados.
- [x] `RenameEngine` com padrões simples e undo.
- [x] `HarvestEngine` com configs simples e casos de sucesso/falha.
- [x] Rodar testes em conjunto com o build:
- [x] `dotnet test` nos projetos de teste.

### 4.2 Refinar uso do JobManager

- [x] Confirmar que todos os fluxos longos usam `JobManager` ou padrão equivalente.
- [x] Garantir passagem de `CancellationToken` até as engines onde fizer sentido.
- [x] Padronizar mensagens de finalização exibidas na UI:
  - [x] Sucesso: sempre com resumo.
  - [x] Erro: mensagem amigável + indicação de logs.

### 4.3 Micro-refactors na WPF

- [x] Revisar usings WPF/WinForms:
  - [x] Remover `System.Windows.Forms` onde não for mais necessário.
  - [x] Garantir que tipos WPF estejam claramente qualificados quando houver ambiguidade.
- [x] (Opcional) Introduzir `global using` para tipos WPF muito usados:
  - [x] `System.Windows`
  - [x] `System.Windows.Controls`
  - [x] `System.Windows.Media`
- [x] Fazer pequenos ajustes de nomes e organização em code-behinds mais carregados, sem alterar comportamento.

---

## Observações Finais

- As fases são independentes o suficiente para serem executadas em branches separadas.
- Dentro de cada fase, é possível priorizar os itens que trazem maior benefício imediato (por exemplo, primeiro resolver dependências vulneráveis e padronizar erros, depois partir para testes automatizados).
