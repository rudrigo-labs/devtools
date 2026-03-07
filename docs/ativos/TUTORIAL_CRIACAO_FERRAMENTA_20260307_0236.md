# Tutorial - Criacao de Ferramenta (Padrao Oficial)

## Objetivo
Usar um processo unico para criar qualquer ferramenta no DevTools sem perder arquitetura, padrao e rastreabilidade.

## Pre-condicoes
1. Ler regras da raiz (`AGENT_RULES_INDEX.md` e `.ai/*`).
2. Confirmar fluxo arquitetural oficial:
   - `Host -> Tool -> Infrastructure -> Database`
3. Confirmar escopo da fase atual (ferramenta alvo).

## Passo a passo oficial
1. Criar demanda no pacote minimo:
   - `DEFINICAO_<TEMA>_yyyyMMdd_HHmm.md`
   - `PENDENCIAS_<TEMA>_yyyyMMdd_HHmm.md`
   - `LOG_DEMANDA_<TEMA>_yyyyMMdd_HHmm.md`
   - atualizar `docs/controle/REGISTRO_DEMANDAS.md`

2. Definir entidade da ferramenta:
   - criar `*Entity` em `src/Tools/DevTools.<Tool>/Models/`
   - herdar de `ToolEntityBase` (Core)
   - incluir propriedades especificas da ferramenta
   - incluir `IsDefault` quando houver selecao de configuracao padrao

3. Definir contratos de Tool:
   - `*ExecutionRequest`
   - `*ExecutionResult`
   - `I*EntityRepository` em `Repositories`

4. Implementar validacao da Tool:
   - `*EntityValidator`
   - `*ExecutionRequestValidator`
   - regras obrigatorias e mensagens de erro padrao

5. Implementar Engine da Tool:
   - `*Engine : IDevToolEngine<TRequest, TResult>`
   - sem acesso direto a banco/UI
   - retorna `RunResult` padrao

6. Implementar Service da entidade:
   - `*EntityService`
   - orquestra validacao + repositorio
   - garante `Id` (slug) e datas UTC

7. Implementar repositorio concreto na Infrastructure:
   - criar `*EntityRepository` em `src/Infrastructure/.../Persistence/Repositories`
   - mapear persistencia na tabela canonica (`tool_configurations`) com `payload_json` ou tabela dedicada quando necessario
   - manter transacao para operacoes criticas (ex: `SetDefault`)

8. Integrar no Host (WPF):
   - Host apenas monta request/entidade e chama Service/Engine
   - regra de negocio continua na Tool
   - sem logica de dominio no code-behind/viewmodel

9. Migrations (no fechamento da integracao):
   - gerar migration da Infrastructure
   - aplicar no banco alvo
   - validar sem fallback silencioso para JSON em persistencia funcional

10. Validacao final:
    - rodar build dos projetos envolvidos
    - rodar testes relevantes
    - registrar evidencias no `LOG_DEMANDA`
    - atualizar status em `PENDENCIAS` e `REGISTRO_DEMANDAS`
    - registrar decisoes em `docs/context-log.md`

## Convencoes de nomenclatura (obrigatorias)
1. Entidade de ferramenta: `*Entity`
2. Repositorio: `I*EntityRepository` e `*EntityRepository`
3. Service: `*EntityService`
4. Validator: `*EntityValidator`
5. Nada de sufixo `Configuration` para entidade da ferramenta.

## Regra de ouro
Fechar 100% uma ferramenta por vez (fim-a-fim) antes de iniciar outra.

