# Context Log

## 2026-03-06 13:15
- Integracao das regras de IA e documentacao em andamento.
- Definida adocao de DOCUMENTATION_RULES na ordem de leitura.
- Definida abertura da demanda de refatoracao de entidades/propriedades das ferramentas.

## 2026-03-06 13:42
- Fase 0 da demanda de refatoracao concluida com baseline tecnico (build/teste/documentos).
- Fase 1 concluida com modelo unificado de configuracoes nomeadas por ferramenta.
- Nomenclatura canonica definida: ConfiguracaoNomeada (UI com rotulo de dominio por ferramenta).

## 2026-03-06 13:56
- Fase 2 avancada com contrato unificado (`NamedToolConfiguration`) e mapper de compatibilidade.
- Metadata comum adotada em configuracoes nomeadas (`__meta:*`) para migracao sem quebra.
- Fase 3 iniciada com aplicacao de infraestrutura comum em ToolConfigurationManager/ToolConfigurationUIService/SqliteToolConfigurationStore.
- Validacao tecnica da rodada: build e testes da suite DevTools.Tests executados com sucesso.


## 2026-03-06 14:35
- Remocao da nomenclatura antiga concluida no codigo-fonte (migrada para Configuration).
- Classes/servicos/stores/entidades e nomes de arquivo atualizados para o padrao Configuration.
- Fluxo SSH migrado para TunnelConfiguration (sem residuos da nomenclatura antiga no dominio da ferramenta).

## 2026-03-06 15:07
- Fase 3 avancada para `Harvest`, `Organizer`, `SearchText`, `Rename`, `ImageSplitter` e `Utf8Convert`.
- Painel de configuracoes recebeu cards e formularios dinamicos para essas ferramentas.
- Janelas de execucao dessas ferramentas agora carregam configuracao padrao no bootstrap.
- Validacao tecnica executada com sucesso (build WPF + testes DevTools.Tests).
