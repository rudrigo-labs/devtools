## Plano de Melhorias â€“ DevTools

Este documento organiza as principais melhorias sugeridas para o projeto DevTools, divididas em fases, com checklists prÃ¡ticos para execuÃ§Ã£o.

---

## Fase 1 â€“ Higiene e SeguranÃ§a BÃ¡sica

Objetivo: deixar o projeto saudÃ¡vel sem alterar comportamento funcional.

### 1.1 AtualizaÃ§Ã£o de pacotes vulnerÃ¡veis

- [x] Mapear onde cada pacote Ã© utilizado:
  - [x] `BouncyCastle.Cryptography`
  - [x] `SixLabors.ImageSharp`
  - [x] `System.Security.Cryptography.Pkcs`
- [x] Verificar changelogs e notas de versÃ£o dos pacotes.
- [x] Definir versÃµes alvo que resolvam os CVEs (mÃ­nimo necessÃ¡rio).
- [x] Atualizar versÃµes nos respectivos `.csproj`.
- [x] Rodar:
  - [x] `dotnet restore`
  - [x] `dotnet build` da solution completa.
- [x] Exercitar manualmente os fluxos que dependem desses pacotes:
  - [x] Funcionalidades que usam imagem (ImageSplit / Organizer, se aplicÃ¡vel).
  - [x] Funcionalidades que usam criptografia/certificados (se houver).

### 1.2 Limpeza de warnings de build (quando fizer sentido)

- [x] Listar warnings atuais de build e classificÃ¡-los:
  - [x] Warnings aceitÃ¡veis e conhecidos (por exemplo, `NU1510` em `System.Text.Encoding.CodePages` no `DevTools.Cli`).
  - [x] Warnings que valem correÃ§Ã£o imediata.
- [x] Corrigir warnings triviais:
  - [x] Usings nÃ£o utilizados.
  - [x] APIs obsoletas com substituiÃ§Ã£o direta.
- [x] (Opcional) Marcar com `NoWarn` apenas warnings conscientemente aceitos, se necessÃ¡rio.

---

## Fase 2 â€“ Tratamento de Erros e Logging

Objetivo: padronizar tratamento de erros e uso de logs, sem reescrever telas.

### 2.1 Definir contrato de erro de UI

- [x] Definir como deve ser uma mensagem de erro padrÃ£o:
  - [x] Curta e compreensÃ­vel para o usuÃ¡rio.
  - [x] Detalhes tÃ©cnicos indo para log (nÃ£o para o `MessageBox`).
- [x] Especificar uma funÃ§Ã£o/utilitÃ¡rio central de erro de UI (conceito):
  - [x] Ex.: `UiError.ShowError(string title, string userMessage, Exception ex)` (na prÃ¡tica, `UiMessageService.ShowError`).
  - [x] Internamente:
    - [x] Logar detalhes via `AppLogger`.
    - [x] Mostrar mensagem amigÃ¡vel para o usuÃ¡rio (WPF).

### 2.2 Integrar helper de erro nas janelas principais

- [x] Identificar janelas mais crÃ­ticas em termos de erro:
  - [x] `LogsWindow`
  - [x] `MigrationsWindow`
  - [x] `OrganizerWindow`
  - [x] `HarvestWindow`
  - [x] `SshTunnelWindow`
  - [x] Outras que faÃ§am operaÃ§Ãµes de IO/processos.
- [x] Substituir `MessageBox.Show(ex.Message, ...)` por chamadas ao helper central:
  - [x] Passar tÃ­tulo/contexto significativo.
  - [x] Garantir que detalhes fiquem nos logs.

### 2.3 Padronizar logs de jobs

- [x] Revisar uso de `JobManager`:
  - [x] Garantir log de inÃ­cio de cada job (nome, parÃ¢metros principais).
  - [x] Garantir log de fim de cada job (sucesso/erro, resumo).
- [x] Definir convenÃ§Ã£o de categoria/prefixo de log por ferramenta:
  - [x] Ex.: `[Organizer]`, `[Harvest]`, `[Migrations]`, `[SSHTunnel]`, `[UI]`.
- [x] Ajustar mensagens de conclusÃ£o de job para um formato consistente:
  - [x] Sucesso: resumo ("X processados, Y alterados, Z ignorados").
  - [x] Erro: frase de alto nÃ­vel + indicaÃ§Ã£o para olhar a janela de logs.

---

## Fase 3 â€“ ValidaÃ§Ã£o de Inputs e ConfiguraÃ§Ãµes

Objetivo: tornar validaÃ§Ãµes mais previsÃ­veis e fÃ¡ceis de ajustar no futuro.

### 3.1 Extrair validaÃ§Ãµes por ferramenta

- [x] Para cada janela que dispara engines:
- [x] `HarvestWindow`
- [x] `OrganizerWindow`
- [x] `MigrationsWindow`
- [x] `RenameWindow`
- [x] Outras conforme necessÃ¡rio.
- [x] Criar mÃ©todos explÃ­citos de validaÃ§Ã£o, por exemplo:
- [x] `ValidationResult ValidateInputs()` ou equivalente.
- [x] Concentrar regras de validaÃ§Ã£o nesses mÃ©todos:
- [x] Required de paths.
- [x] Formato de nÃºmeros (score, portas, etc.).
- [x] Campos obrigatÃ³rios para cada aÃ§Ã£o (ex.: nome de migration).

### 3.2 Integrar validaÃ§Ã£o com UI e logging

- [x] Na aÃ§Ã£o principal de cada janela (ex.: `Run`, `Execute`, etc.):
- [x] Chamar mÃ©todo de validaÃ§Ã£o.
- [x] Em caso de erro, usar helper de UI (`UiError` ou similar) ou mensagens padronizadas de atenÃ§Ã£o.
- [x] Garantir que erros de configuraÃ§Ã£o (paths nÃ£o encontrados, JSON invÃ¡lido, etc.) gerem:
- [x] Mensagem amigÃ¡vel para o usuÃ¡rio.
- [x] Registro detalhado no log.

### 3.3 Revisar ConfigService e configuracoes

- [x] Analisar `ConfigService`:
  - [x] Definir claramente seÃ§Ãµes de configuraÃ§Ã£o (SSH, Harvest, Organizer, etc.).
  - [x] Melhorar mensagens quando o arquivo nÃ£o existir / estiver invÃ¡lido.
- [x] Revisar estrutura de configuracoes por ferramenta:
  - [x] Identificar chaves realmente essenciais.
  - [x] Avaliar se hÃ¡ redundÃ¢ncia ou campos pouco usados.

---

## Fase 4 â€“ Robustez das Engines e Refinos de WPF

Objetivo: dar sustentaÃ§Ã£o para refactors futuros e deixar a UI mais limpa.

### 4.1 Testes automatizados nas engines principais

- [x] Criar projeto(s) de teste para engines (se ainda nÃ£o existir):
- [x] Ex.: `DevTools.Organizer.Tests`, `DevTools.Rename.Tests`, etc.
- [x] Cobrir cenÃ¡rios bÃ¡sicos:
- [x] `OrganizerEngine` com pastas de exemplo, incluindo:
- [x] Arquivos a serem movidos.
- [x] Arquivos ignorados.
- [x] `RenameEngine` com padrÃµes simples e undo.
- [x] `HarvestEngine` com configs simples e casos de sucesso/falha.
- [x] Rodar testes em conjunto com o build:
- [x] `dotnet test` nos projetos de teste.

### 4.2 Refinar uso do JobManager

- [x] Confirmar que todos os fluxos longos usam `JobManager` ou padrÃ£o equivalente.
- [x] Garantir passagem de `CancellationToken` atÃ© as engines onde fizer sentido.
- [x] Padronizar mensagens de finalizaÃ§Ã£o exibidas na UI:
  - [x] Sucesso: sempre com resumo.
  - [x] Erro: mensagem amigÃ¡vel + indicaÃ§Ã£o de logs.

### 4.3 Micro-refactors na WPF

- [x] Revisar usings WPF/WinForms:
  - [x] Remover `System.Windows.Forms` onde nÃ£o for mais necessÃ¡rio.
  - [x] Garantir que tipos WPF estejam claramente qualificados quando houver ambiguidade.
- [x] (Opcional) Introduzir `global using` para tipos WPF muito usados:
  - [x] `System.Windows`
  - [x] `System.Windows.Controls`
  - [x] `System.Windows.Media`
- [x] Fazer pequenos ajustes de nomes e organizaÃ§Ã£o em code-behinds mais carregados, sem alterar comportamento.

---

## ObservaÃ§Ãµes Finais

- As fases sÃ£o independentes o suficiente para serem executadas em branches separadas.
- Dentro de cada fase, Ã© possÃ­vel priorizar os itens que trazem maior benefÃ­cio imediato (por exemplo, primeiro resolver dependÃªncias vulnerÃ¡veis e padronizar erros, depois partir para testes automatizados).

