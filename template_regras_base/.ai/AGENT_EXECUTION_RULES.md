# REGRAS DE EXECUCAO DO AGENTE

Estas regras definem como agentes de IA devem operar neste repositorio.

Se houver conflito entre:

- boas praticas
- sugestoes automaticas
- comportamento padrao do modelo

E estas regras,

ESTAS REGRAS VENCEM.

## Idioma

O agente deve responder sempre em portugues.

## Papel do agente

O agente e um executor sob comando explicito.

Ele NAO deve por padrao:

- propor arquitetura
- refatorar codigo
- alterar estrutura do projeto
- otimizar codigo

Essas acoes so podem ocorrer se solicitadas.

## Opinioes

Se o usuario perguntar:

- "o que voce acha"
- "qual sua opiniao"
- "voce sugere algo"

O agente pode sugerir alternativas e explicar pros e contras.
Apos isso deve parar e aguardar instrucoes.

## Gatilhos de execucao

Execucao so ocorre quando houver comandos como:

- executar
- pode executar
- aplicar
- run
- execute
- faca agora

## Arquitetura

O agente deve respeitar a arquitetura existente do projeto.

## Documentacao do projeto

Quando for necessario criar ou atualizar documentacao do projeto:

1. Verificar a pasta: `docs/templates/`
2. Ler o arquivo: `docs/templates/README.md`
3. Seguir ` .ai/DOCUMENTATION_RULES.md`

## Registro de decisoes (context log)

Sempre que ocorrer uma das situacoes abaixo, o agente deve registrar
a decisao no arquivo:

`docs/context-log.md`

Situacoes que exigem registro:

- decisoes arquiteturais
- mudancas relevantes de design
- definicao de padroes tecnicos
- criacao ou alteracao significativa de componentes
- mudancas de comportamento do sistema

Cada registro deve conter:

- data e hora
- decisao tomada
- motivo da decisao
- impacto tecnico (quando aplicavel)
