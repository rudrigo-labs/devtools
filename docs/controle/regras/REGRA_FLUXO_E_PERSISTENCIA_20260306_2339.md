# REGRA - Fluxo e Persistencia Canonicos

- Status: Ativa
- Criado em: 2026-03-06 23:39
- Escopo: Todas as novas tarefas, refatoracoes e novas ferramentas

## Objetivo
Garantir que nenhuma implementacao saia do fluxo arquitetural definido e que a persistencia siga um padrao unico.

## Fluxo obrigatorio
1. `Host (WPF/CLI)` somente coleta entrada e dispara acao.
2. `Tool.Engine` executa o caso de uso.
3. `Tool.Repositories` (interfaces de dominio) abstraem acesso a dados.
4. `Infrastructure` implementa persistencia e detalhes tecnicos.
5. Retorno sempre no caminho inverso.

Fluxo canonico:
`Host -> Tool.Engine -> Tool.Repositories -> Infrastructure -> Banco`

## Regra de persistencia
1. Banco (`SQLite`) e a fonte de verdade para:
   - dados de dominio,
   - configuracoes nomeadas por ferramenta,
   - estado persistente de execucao.
2. Arquivo (`JSON`) fica restrito a:
   - configuracao global da aplicacao/host,
   - parametros de ambiente,
   - artefatos de saida (export, report, snapshot, log tecnico).
3. Arquivo NAO substitui persistencia de dominio.

## Proibicoes
1. Host nao implementa regra de negocio.
2. Host nao acessa banco direto.
3. Tool nao referencia WPF.
4. Tool nao referencia `DbContext`/EF/SQLite direto.
5. Infrastructure nao referencia Presentation.

## Aplicacao obrigatoria
1. Toda nova ferramenta deve nascer nesse fluxo.
2. Toda refatoracao deve convergir para esse fluxo.
3. Desvio so pode ocorrer com definicao documentada e aprovada na pasta `docs/definicoes`.

## Checklist rapido para iniciar tarefa
- A chamada parte do Host e termina na Tool.
- Regras de negocio estao no Engine.
- Persistencia de dominio esta no banco.
- Arquivo foi usado apenas para global/ambiente/artefato.
