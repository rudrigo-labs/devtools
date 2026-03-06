# Politica de Gestao de Documentos - 20260305_2319

## Regras Gerais
1. Nao deletar documentos historicos.
2. Documento concluido deve ser movido para `docs/resolvidos/`.
3. Todo documento novo deve ser registrado no indice de controle.
4. Historico de alteracao deve ficar no proprio documento (secao de status/encerramento).
5. Todo documento operacional deve usar sufixo `yyyyMMdd_HHmm`.

## Regra Obrigatoria por Demanda
Toda demanda, pequena ou grande, deve nascer com pacote minimo de documentacao.

Pacote minimo:
1. Definicao da demanda em `docs/definicoes/`.
2. Documento de execucao (pendencias/checklist/fase) em `docs/pendencias/`.
3. Log de execucao da demanda (decisoes, desvios, bloqueios e validacoes) em `docs/pendencias/`.
4. Registro da demanda em `docs/controle/REGISTRO_DEMANDAS.md`.

Padrao de nomes recomendado:
- `DEFINICAO_<TEMA>_yyyyMMdd_HHmm.md`
- `PENDENCIAS_<TEMA>_yyyyMMdd_HHmm.md`
- `LOG_DEMANDA_<TEMA>_yyyyMMdd_HHmm.md`

## Regra de Rastreabilidade
1. Nenhuma demanda inicia sem definicao registrada.
2. Toda alteracao de escopo deve ser registrada no log da demanda.
3. Toda conclusao parcial/final deve atualizar:
   - documento de execucao da demanda
   - `docs/controle/REGISTRO_DEMANDAS.md`
4. Ao encerrar demanda, manter historico completo (sem sobrescrever contexto antigo).

## Estrutura Oficial
- `docs/ativos/`: referencia ativa (manual, guias, tecnico)
- `docs/definicoes/`: decisoes e definicoes de arquitetura/escopo
- `docs/pendencias/`: backlog, checklists e logs de demanda em andamento
- `docs/relatorios/`: relatorios de teste/auditoria/release
- `docs/resolvidos/`: historico encerrado
- `docs/templates/`: modelos padrao
- `docs/controle/`: politicas, indices e registro mestre

## Ciclo de Vida
- Aberto -> Em andamento -> Concluido -> Arquivado

## Abrangencia
Este padrao vale para qualquer demanda neste projeto e deve ser reutilizado como padrao base nos demais projetos.

## Data de criacao
2026-03-05 23:19
