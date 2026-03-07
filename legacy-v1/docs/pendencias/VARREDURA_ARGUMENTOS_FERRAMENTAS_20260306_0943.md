# Varredura Profunda - Resquicios de Argumentos nas Ferramentas

Data: 2026-03-06 09:43
Escopo: `src/Presentation`, `src/Tools`, `src/Core`
Objetivo: identificar onde "argumentos adicionais" ainda estao expostos/consumidos e separar o que pode ficar guardado (sem uso) do que precisa ser bloqueado.

## Resumo Executivo

Diagnostico:
- Existem **2 canais ativos de argumentos** em producao hoje:
  - `Migrations` (`AdditionalArgs`)
  - `Ngrok` (`AdditionalArgs`)
- Esses canais estao ativos em **UI + persistencia + runtime**.
- Apenas ocultar campo de UI **nao resolve**: os argumentos continuam sendo aplicados se vierem de configuracao/configuracao salvo.

Minha opiniao tecnica:
- Faz sentido manter o codigo "guardado".
- Para cumprir sua regra de negocio (nao usar), precisa desativar em 3 camadas ao mesmo tempo:
  - exibicao (UI)
  - gravacao/leitura de valor (config/configuracao)
  - consumo no runtime (engine/command builder)

## Mapa de Pontos Ativos (onde argumentos ainda sao usados)

### 1) Migrations

UI (janela da ferramenta):
- `src/Presentation/DevTools.Presentation.Wpf/Views/MigrationsWindow.xaml`
  - `AdditionalArgsInput` + chips de atalho.
- `src/Presentation/DevTools.Presentation.Wpf/Views/MigrationsWindow.xaml.cs`
  - carrega `_resolvedSettings.AdditionalArgs`
  - valida `TryValidateAdditionalArgs`
  - salva em request/settings/configuration.

UI (configuracoes gerais):
- `src/Presentation/DevTools.Presentation.Wpf/Views/MainWindow.xaml`
  - `MigArgsInput`.
- `src/Presentation/DevTools.Presentation.Wpf/Views/MainWindow.xaml.cs`
  - `LoadMigrationsConfig`/`SaveMigrationsSettings` usam `AdditionalArgs`.

Configuracoes/projetos:
- `src/Presentation/DevTools.Presentation.Wpf/Services/ToolConfigurationUIService.cs`
  - campo `additional-args` para Migrations.

Runtime (execucao real):
- `src/Tools/DevTools.Migrations/Models/MigrationsSettings.cs`
  - propriedade `AdditionalArgs`.
- `src/Tools/DevTools.Migrations/Engine/EfCommandBuilder.cs`
  - `AppendAdditionalArgs(cmd, s.AdditionalArgs)` em Add/Update.

Conclusao: hoje Migrations **usa argumentos de verdade**.

### 2) Ngrok

UI (configuracoes gerais):
- `src/Presentation/DevTools.Presentation.Wpf/Views/MainWindow.xaml`
  - `NgrokArgsInput`.
- `src/Presentation/DevTools.Presentation.Wpf/Views/MainWindow.xaml.cs`
  - `LoadNgrokConfig`/`SaveNgrokSettings` le/grava `AdditionalArgs`.

Configuracoes/conexoes:
- `src/Presentation/DevTools.Presentation.Wpf/Services/ToolConfigurationUIService.cs`
  - campo `additional-args` para Ngrok.

Persistencia:
- `src/Tools/DevTools.Ngrok/Models/NgrokSettings.cs`
  - propriedade `AdditionalArgs`.
- `src/Tools/DevTools.Ngrok/Services/NgrokJsonSettingsStore.cs`
- `src/Tools/DevTools.Ngrok/Services/NgrokSqliteSettingsStore.cs`

Runtime (execucao real):
- `src/Tools/DevTools.Ngrok/Engine/NgrokTunnelEngine.cs`
  - `ParseAdditionalArgs(settings.AdditionalArgs)` e injeta no start.

Conclusao: hoje Ngrok **usa argumentos de verdade**.

## Resquicios Relacionados (nao exatamente argumentos)

Achados adicionais de arquitetura (nao bloqueiam a regra de argumentos, mas indicam ruido de evolucao):
- Algumas janelas recebem `ToolConfigurationManager` no construtor e nao usam no runtime atual (ex.: Harvest, Rename, SearchText, SSH em partes).
- Existe tela de configuracoes/conexoes central no `MainWindow` para Migrations/Snapshot/SSH/Ngrok; parte dela esta desconectada de algumas janelas em runtime.

## O que pode ficar guardado (sem remover codigo)

Pode manter sem problema:
- propriedades `AdditionalArgs` nos modelos
- metodos de parse/append nos engines
- stores JSON/SQLite preparados

Desde que:
- a aplicacao **nao exponha** e **nao consuma** esses valores no fluxo normal.

## Como desativar uso sem apagar codigo (plano recomendado)

### Fase 1 - Bloqueio de UI (rapido)
- Ocultar campos de argumentos em:
  - MigrationsWindow
  - MainWindow (config Migrations e Ngrok)
  - ToolConfigurationUIService (formularios dinamicos)
- Remover chips de argumentos da UI.

### Fase 2 - Bloqueio de persistencia de entrada
- Parar de salvar argumentos vindos da UI em:
  - `MigrationsSettings.AdditionalArgs`
  - `NgrokSettings.AdditionalArgs`
  - `ToolConfiguration.Options["additional-args"]`

### Fase 3 - Bloqueio de runtime (obrigatorio)
- Migrations: forcar `AdditionalArgs = null` antes de montar comando.
- Ngrok: ignorar `settings.AdditionalArgs` no `StartTunnelAsync`.

### Fase 4 - Compatibilidade com dados antigos
- Se existir valor legado salvo:
  - ler normalmente, mas ignorar em runtime.
- Opcional: limpar silenciosamente no save.

## Risco se fizer so "esconder campo"

Se esconder apenas UI:
- valor antigo salvo continua ativo
- comando continua recebendo argumentos
- regra "nao usar argumentos" fica quebrada sem aparecer na tela

## Parecer Final

Estado atual: **nao esta conforme** sua regra de negocio (argumentos ainda ativos em Migrations e Ngrok).

Recomendacao: implementar o bloqueio em 3 camadas (UI + persistencia + runtime), mantendo codigo guardado para futura reativacao.



