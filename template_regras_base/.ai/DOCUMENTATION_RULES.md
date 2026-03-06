# DOCUMENTATION RULES

Regras padrao de documentacao reutilizaveis entre projetos.

## Pacote minimo por demanda
Toda demanda deve ter:

1. DEFINICAO em `docs/definicoes/`
2. EXECUCAO em `docs/pendencias/`
3. LOG em `docs/pendencias/`
4. REGISTRO MESTRE em `docs/controle/`

Padrao de nomes:

- `DEFINICAO_<TEMA>_yyyyMMdd_HHmm.md`
- `PENDENCIAS_<TEMA>_yyyyMMdd_HHmm.md`
- `LOG_DEMANDA_<TEMA>_yyyyMMdd_HHmm.md`

## Rastreabilidade

- Nenhuma demanda inicia sem definicao.
- Mudancas de escopo entram no LOG da demanda.
- Encerramento de fase ou demanda atualiza o registro mestre.
- Historico nunca deve ser apagado.

## Estrutura recomendada

- `docs/ativos/`
- `docs/definicoes/`
- `docs/pendencias/`
- `docs/relatorios/`
- `docs/resolvidos/`
- `docs/templates/`
- `docs/controle/`

## Regra da raiz

Na raiz do repositorio devem permanecer apenas arquivos essenciais
(de acordo com a politica do projeto).
