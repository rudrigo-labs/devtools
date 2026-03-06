# DevTools - Plano de Implementacao da Modal Padrao

Data: 2026-03-05

## Objetivo

Criar uma modal padrao, propria do projeto, para substituir a dependencia atual de dialogos e padronizar UX.

## Escopo

Implementar:

- `DevToolsDialogWindow` (janela modal reutilizavel)
- `IAppDialogService` + `AppDialogService`
- Adaptacao do `UiMessageService` para usar o novo servico
- Migracao inicial de `Confirm` (exclusao de nota, encerramento e confirmacoes gerais)

Fora de escopo nesta etapa:

- migrar 100% de todas as mensagens em um unico commit
- empacotar como NuGet

## Requisitos Funcionais

1. A modal deve aceitar:
   - titulo
   - mensagem
   - 1 botao (OK/Fechar) ou 2 botoes (Sim/Nao, Confirmar/Cancelar)
2. Deve abrir como modal real (`ShowDialog`) e bloquear a janela de tras.
3. Deve retornar resultado para fluxo de confirmacao (`true/false`).
4. Deve usar o tema visual atual (cores, fontes e botoes do projeto).

## Arquitetura Proposta

### 1) Modelo de dialogo

- `DialogKind`: `Info`, `Warning`, `Error`, `Confirm`
- `DialogOptions`:
  - `Title`
  - `Message`
  - `PrimaryText`
  - `SecondaryText` (opcional)
  - `Kind`

### 2) Servico

- `IAppDialogService`
  - `ShowInfo(...)`
  - `ShowWarning(...)`
  - `ShowError(...)`
  - `Confirm(...)`
- `AppDialogService` (implementacao WPF via `DevToolsDialogWindow`)

### 3) Janela

- `Views/DevToolsDialogWindow.xaml`
- Layout simples:
  - header (titulo)
  - corpo (mensagem)
  - rodape (botoes)
- Botao primario e secundario usando estilos existentes:
  - `DevToolsPrimaryButton`
  - `DevToolsSecondaryButton`

## Fases

### Fase 1 - Base da modal

- Criar janela `DevToolsDialogWindow`
- Parametrizar titulo/mensagem/botoes
- Garantir retorno de resultado

### Fase 2 - Servico padrao

- Criar `IAppDialogService` e `AppDialogService`
- Definir ponto unico de abertura da modal
- Garantir owner correto (janela ativa)

### Fase 3 - Integracao inicial

- Refatorar `UiMessageService` para delegar ao `AppDialogService`
- Migrar `Confirm` primeiro
- Validar exclusao de notas e fluxos de encerramento

### Fase 4 - Expansao

- Migrar `ShowInfo/Warning/Error`
- Remover fallback legado gradualmente

## Esforco por Arquivo

- `src/Presentation/DevTools.Presentation.Wpf/Views/DevToolsDialogWindow.xaml` - medio
- `src/Presentation/DevTools.Presentation.Wpf/Views/DevToolsDialogWindow.xaml.cs` - baixo
- `src/Presentation/DevTools.Presentation.Wpf/Services/IAppDialogService.cs` - baixo
- `src/Presentation/DevTools.Presentation.Wpf/Services/AppDialogService.cs` - medio
- `src/Presentation/DevTools.Presentation.Wpf/Services/UiMessageService.cs` - medio/alto

## Criterios de Aceite

1. Confirmacao de exclusao de nota abre modal padrao com 2 botoes.
2. Modal bloqueia a janela de tras.
3. Resultado da modal controla corretamente a acao (`Confirmar` executa / `Cancelar` aborta).
4. Visual consistente com tema atual.
5. Sem regressao nos testes de integracao existentes.

## Riscos e Mitigacao

- Risco: conflito de owner/dispatcher em testes WPF.
  - Mitigacao: centralizar owner no `AppDialogService`.
- Risco: regressao em chamadas existentes.
  - Mitigacao: migracao por fases, iniciando por `Confirm`.

## Ordem Recomendada de Execucao

1. Fechar pendencias atuais de configuracao/Ngrok.
2. Executar Fase 1 e Fase 2.
3. Integrar Fase 3 (Confirm).
4. Validar com testes.
5. Migrar demais mensagens (Fase 4).
