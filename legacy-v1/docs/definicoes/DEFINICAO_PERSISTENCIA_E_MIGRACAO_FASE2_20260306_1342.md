# Definicao Fase 2 - Persistencia, Contratos e Migracao

- Data/Hora: 2026-03-06 13:42
- Demanda: `DEFINICAO_REFATORACAO_ENTIDADES_FERRAMENTAS_20260306_1315.md`

## Mapeamento atual de persistencia
1. `SettingsService` (estado local de UI)
2. `ConfigService` (secoes globais JSON/SQLite)
3. `ToolConfigurationManager` + stores (configuracoes nomeadas por ferramenta)
4. Store proprio do Ngrok (`NgrokSettings` JSON/SQLite)

## Contrato unificado adotado
Implementado contrato comum baseado em `NamedToolConfiguration`, com mapeamento de compatibilidade
para o modelo atual (`ToolConfiguration`) sem quebra de dados.

Arquivos da implementacao:
- `src/Core/DevTools.Core/Models/NamedToolConfiguration.cs`
- `src/Core/DevTools.Core/Models/ToolConfiguration.cs` (campos comuns ampliados)
- `src/Core/DevTools.Core/Configuration/ToolConfigurationManager.cs` (normalizacao + load/save de configuracoes)
- `src/Presentation/DevTools.Presentation.Wpf/Persistence/Stores/SqliteToolConfigurationStore.cs` (metadata compativel)
- `src/Presentation/DevTools.Presentation.Wpf/Services/ToolConfigurationUIService.cs` (normalizacao na camada de orquestracao)

## Estrategia de migracao adotada
1. Nao quebrar o formato atual imediatamente.
2. Persistir metadata comum dentro de options com chaves reservadas (`__meta:*`).
3. Permitir leitura de dados legados sem metadata.
4. Preencher defaults na carga (`ToolSlug`, `CreatedUtc`, `IsActive`).

## Pendencia da fase
- Remover persistencia indevida da apresentacao (item ainda pendente para etapa dedicada da Fase 2).

