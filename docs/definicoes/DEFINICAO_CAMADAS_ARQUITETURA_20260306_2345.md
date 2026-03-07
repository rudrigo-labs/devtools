# DEFINICAO - Camadas da Arquitetura

- Status: Aberto
- Criado em: 2026-03-06 23:45
- Escopo: Todas as ferramentas novas e refatoradas

## Objetivo
Definir um modelo unico de camadas para evitar acoplamento e reduzir retrabalho.

## Camadas
1. `Host` (WPF/CLI)
- Coleta entrada.
- Monta request.
- Chama a ferramenta.
- Exibe resultado.

2. `Core`
- Contratos compartilhados.
- Tipos comuns (result, erros, abstracoes).
- Sem regra de negocio especifica de ferramenta.

3. `Tools`
- Regra de negocio.
- Engine por ferramenta.
- Validacao da ferramenta.
- Repositories como interfaces de dominio.

4. `Infrastructure`
- Persistencia e detalhes tecnicos.
- Implementacao concreta de repositorios.
- SQLite, EF, IO e integracoes tecnicas.

## Direcao de chamada (obrigatoria)
`Host -> Tool.Engine -> Tool.Repositories -> Infrastructure -> Banco`

Retorno:
`Banco -> Infrastructure -> Tool.Engine -> Host`

## Regras de dependencia
1. Host nao referencia Infrastructure.
2. Host nao implementa regra de negocio da ferramenta.
3. Tool nao referencia WPF.
4. Tool nao depende de DbContext/EF/SQLite direto.
5. Infrastructure nao referencia Presentation.
6. Core nao conhece UI nem persistencia concreta.

## Regra de persistencia
1. Banco: dados de dominio e configuracoes nomeadas.
2. Arquivo: apenas configuracao global, ambiente e artefatos de saida.

## Criterio de aceite
- Qualquer ferramenta deve conseguir rodar por WPF ou CLI sem alterar regra de negocio.
