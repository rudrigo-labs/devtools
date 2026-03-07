# Controle de Documentos de Pendencias - 20260305_2309

## Objetivo
Controlar checklist, fases e pendencias por documento, evitando novas varreduras completas desnecessarias.

## Convencao de Nome
- Pasta obrigatoria: `docs/pendencias/`
- Padrao de arquivo: `TIPO_TEMA_yyyyMMdd_HHmm.md`
- Exemplos:
  - `PENDENCIAS_CONFIGURACOES_20260305_2309.md`
  - `CHECKLIST_TESTES_20260305_2315.md`
  - `FASE_01_MIGRATIONS_20260305_2320.md`
  - `LOG_DEMANDA_CONFIGURACOES_20260306_1200.md`

## Regra de Pacote Minimo por Demanda
Toda demanda deve ter, no minimo:
1. `DEFINICAO_<TEMA>_yyyyMMdd_HHmm.md` em `docs/definicoes/`
2. `PENDENCIAS_<TEMA>_yyyyMMdd_HHmm.md` em `docs/pendencias/`
3. `LOG_DEMANDA_<TEMA>_yyyyMMdd_HHmm.md` em `docs/pendencias/`
4. Registro em `docs/controle/REGISTRO_DEMANDAS.md`

## Regras de Uso
- Todo novo documento de pendencia/checklist/fase deve ter sufixo de data/hora.
- Ao concluir um documento, atualizar status aqui antes de criar o proximo.
- Nao apagar historico; marcar como `Concluido` e registrar data.
- Toda mudanca de escopo durante execucao deve entrar no `LOG_DEMANDA`.

## Inventario Atual
| Documento | Tipo | Tema | Status | Criado Em | Concluido Em | Observacao |
| --- | --- | --- | --- | --- | --- | --- |
| `PENDENCIAS_CONFIGURACOES_20260305_2309.md` | Pendencias | Configuracoes de ferramentas | Em andamento | 2026-03-05 23:09 | - | Base para execucao por fases |
| `PENDENCIAS_REFATORACAO_ENTIDADES_FERRAMENTAS_20260306_1315.md` | Pendencias | Refatoracao entidades/propriedades | Em andamento | 2026-03-06 13:15 | - | Demanda principal atual |
| `LOG_DEMANDA_REFATORACAO_ENTIDADES_FERRAMENTAS_20260306_1315.md` | Log | Refatoracao entidades/propriedades | Em andamento | 2026-03-06 13:15 | - | Rastreabilidade cronologica da demanda |

## Pipeline Sugerido
1. Criar documento de definicao da demanda.
2. Criar documento de pendencias (escopo fechado).
3. Criar log de demanda e registrar decisoes.
4. Executar uma fase por vez.
5. Atualizar este controle e o registro mestre.

## Modelo de Status
- `Aberto`: ainda nao iniciado.
- `Em andamento`: possui execucao ativa.
- `Bloqueado`: depende de decisao/entrada externa.
- `Concluido`: finalizado e validado.
- `Arquivado`: historico sem acao futura.

## Proxima Acao
- Executar Fase 0/Fase 1 de `PENDENCIAS_REFATORACAO_ENTIDADES_FERRAMENTAS_20260306_1315.md`.
