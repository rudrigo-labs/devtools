# DEFINICAO - Arquitetura de Persistencia e Orquestracao das Ferramentas

- Status: Ativo
- Criado em: 2026-03-06 18:56
- Contexto: consolidacao dos dois textos-base de persistencia + ajustes criticos definidos no projeto DevTools
- Relacionado a: `DEFINICAO_REFATORACAO_ENTIDADES_FERRAMENTAS_20260306_1315.md`

## Objetivo
Definir a arquitetura alvo de persistencia e orquestracao do DevTools, removendo ambiguidade de responsabilidades entre UI, dominio das tools e acesso a dados.

## Base consolidada (o que foi incorporado)
1. Host nao deve conter dominio.
2. Host nao deve ser dono de persistencia de negocio.
3. Tools concentram regras de negocio e contratos de execucao.
4. Core contem contratos e modelos compartilhados.
5. Persistencia deve ficar isolada em camada de infraestrutura, com banco unico.
6. Migrations devem ser centralizadas em um unico historico.
7. Termo `Profile` deixa de existir no dominio funcional; usar `ConfiguracaoNomeada` (rotulo de UI pode variar: Projeto, Conexao, Configuracao).

## Estado atual (as-is no repositorio)
Estrutura atual:
- `src/Core/DevTools.Core`
- `src/Tools/*`
- `src/Presentation/DevTools.Presentation.Wpf`
- `src/Cli/*`

Ponto tecnico atual:
- Parte da persistencia ainda esta em `src/Presentation/DevTools.Presentation.Wpf/Persistence`.
- Existe `DevToolsDbContext` no projeto WPF.
- Existem stores SQLite/JSON ainda conectados ao fluxo atual.

Conclusao do estado atual:
- O projeto ja avancou no modelo unificado de configuracoes.
- Ainda ha acoplamento de persistencia na camada de apresentacao, que deve ser removido na etapa estrutural final.

## Arquitetura alvo (to-be)
Estrutura alvo:

```text
src
- Core
  - DevTools.Core
    - Models
    - Configuration
    - Abstractions
- Tools
  - DevTools.<ToolA>
    - Models
    - Engine
    - Entities (quando persistir)
    - Repositories (interfaces + implementacoes de dominio)
  - ...
- Infrastructure
  - DevTools.Infrastructure
    - Persistence
      - DevToolsDbContext
      - ConnectionFactory
      - Stores
      - Migrations
- Presentation
  - DevTools.Presentation.Wpf
- Cli
  - DevTools.Cli
```

## Regras arquiteturais obrigatorias
1. WPF e CLI sao hosts de orquestracao (entrada/saida), nao de dominio.
2. Host nao acessa banco diretamente.
3. Tool recebe request, aplica regra de negocio e usa repositorio/servico.
4. DbContext unico do sistema, centralizado na infraestrutura.
5. Historico de migrations unico, centralizado na infraestrutura.
6. Persistencia de configuracao de negocio deve convergir para SQLite unico.
7. `settings.json` fica restrito a estado de UI temporario (geometria/ultima tela), sem regra de negocio.
8. Configuracao nomeada e o conceito canonico para itens salvos de ferramenta.

## Banco de dados
Modelo alvo:
- Banco unico: `DevTools.db`.
- Tabelas de tool no mesmo banco, com separacao por entidade.
- Tabela (ou conjunto) de configuracoes nomeadas unificadas por `ToolSlug`.

Exemplos de grupos de dados:
- Configuracoes nomeadas das ferramentas.
- Notes e metadados de notas.
- Configuracoes tecnicas globais estritamente necessarias.

## Modelo funcional de configuracao
Contrato base comum (conceitual):
- `Id` (tecnico)
- `ToolSlug`
- `Name`
- `Description`
- `IsActive`
- `IsDefault`
- `CreatedAtUtc`
- `UpdatedAtUtc`
- `Payload` (json tipado por ferramenta)

Regras:
1. `ToolSlug + Name` unico.
2. Sem conceito funcional de `Profile`.
3. Cada item salvo deve representar uma configuracao completa para execucao da ferramenta.

## Fluxo oficial de execucao

```text
Host (WPF/CLI)
   -> monta Request
   -> chama Tool Engine
   -> Tool valida regras
   -> Tool persiste/consulta via repositorio
   -> repositorio usa Infrastructure (DbContext/Store)
   -> retorna resultado ao Host
```

## Decisoes de migracao
1. Migracao incremental, sem quebra abrupta.
2. Onde houver compatibilidade legado, migrar e normalizar para modelo canonico.
3. Remover residuos de nomenclatura antiga (`Profile`) em codigo, UI e documentos de arquitetura.
4. Ao final da migracao, Presentation nao deve conter `DbContext`, entidades de persistencia ou stores de negocio.

## Criterios de aceite desta definicao
1. Toda nova implementacao deve seguir este desenho.
2. Qualquer excecao deve ser registrada no log da demanda com justificativa tecnica.
3. Documentos de arquitetura e pendencias devem referenciar esta definicao como base estrutural.
