# Documentacao DevTools

## Estrutura Oficial
- `docs/ativos/` -> documentacao ativa de uso e tecnica
- `docs/ativos/planejamento/` -> kickoff e planos em execucao
- `docs/definicoes/` -> definicoes de demanda, arquitetura e escopo
- `docs/pendencias/` -> pendencias abertas, checklists e logs de demanda
- `docs/relatorios/` -> auditorias, testes e prontidao de release
- `docs/resolvidos/` -> historico concluido
- `docs/controle/` -> indice mestre, politica e registro de demandas
- `docs/templates/` -> templates padrao

## Regra de Demanda (Obrigatoria)
Toda demanda deve seguir pacote minimo:
1. Definicao (`docs/definicoes/DEFINICAO_<TEMA>_yyyyMMdd_HHmm.md`)
2. Execucao (`docs/pendencias/PENDENCIAS_<TEMA>_yyyyMMdd_HHmm.md`)
3. Log (`docs/pendencias/LOG_DEMANDA_<TEMA>_yyyyMMdd_HHmm.md`)
4. Registro no controle (`docs/controle/REGISTRO_DEMANDAS.md`)

## Ordem de Uso (Quem usa primeiro)
1. `DEFINICAO_*`: cria primeiro, para fechar escopo e regra.
2. `PENDENCIAS_*`: cria segundo, para quebrar execucao em itens.
3. `LOG_DEMANDA_*`: usa durante a execucao, registrando decisoes e mudancas.
4. `REGISTRO_DEMANDAS.md`: atualiza por ultimo, como painel mestre de controle.

## Regras da Raiz
- Permanecem na raiz apenas: `README.md` e `LICENSE`
- Demais `.md` devem ficar dentro de `docs/`

## Entradas principais
- Manual detalhado do usuario: `docs/ativos/MANUAL.md`
- Guia tecnico consolidado: `docs/ativos/TechnicalDoc.md`
- Configuracoes das ferramentas: `docs/ativos/CONFIGURACOES_FERRAMENTAS_DETALHADO.md`
- Pendencias atuais: `docs/pendencias/`
- Indice e politicas: `docs/controle/`

