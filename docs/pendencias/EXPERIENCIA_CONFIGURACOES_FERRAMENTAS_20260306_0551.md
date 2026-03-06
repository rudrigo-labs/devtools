# Experiencia de Configuracoes por Ferramenta - $ts

## Objetivo
Definir um fluxo de experiencia para uso de configuracoes/projetos ao abrir ferramentas, reduzindo retrabalho de preenchimento manual e mantendo opcao de uso sem configuracao.

## Escopo
- Ferramentas com suporte a configuracao/projeto.
- Nao altera regras de execucao das ferramentas.
- Foco em UX de entrada (momento do clique no card da ferramenta).

## Proposta de Fluxo
1. Usuario clica no card da ferramenta.
2. Se existir configuracao/projeto salvo para a ferramenta e o prompt estiver habilitado:
   - Abrir modal "Selecionar Configuracao/Projeto".
   - Acoes:
     - `Selecionar e continuar`
     - `Seguir sem configuracao`
3. Se nao existir configuracao/projeto salvo:
   - Abrir ferramenta diretamente no modo sem configuracao.

## Modal: Selecionar Configuracao/Projeto
- Titulo: `Selecionar Configuracao` ou `Selecionar Projeto` (conforme ferramenta).
- Conteudo:
  - Lista de configuracoes/projetos salvos.
  - Destaque para o padrao.
- Botoes:
  - `Selecionar e continuar`
  - `Seguir sem configuracao`
  - `Cancelar`

## Fluxo de "Seguir sem configuracao"
Ao escolher `Seguir sem configuracao`:
- Exibir dica contextual (uma vez por ferramenta):
  - "Dica: com configuracao/projeto voce preenche tudo mais rapido."
- Exibir checkbox: `Nao exibir novamente`.
- Persistir a preferencia quando marcado.

## Regras de Comportamento
- O prompt de selecao so aparece se:
  - a ferramenta suporta configuracao/projeto;
  - existe pelo menos 1 configuracao/projeto salvo;
  - preferencia de exibicao do prompt esta habilitada.
- Se usuario clicar `Selecionar e continuar` sem selecionar item:
  - validar inline e impedir continuar.
- `Nao exibir novamente` deve ser por ferramenta (nao global).

## Preferencias de UX (persistencia)
Sugestao de chaves por ferramenta:
- `ShowConfigurationSelectorOnToolOpen.{ToolId}` (default: true)
- `HideNoConfigurationTip.{ToolId}` (default: false)

## Minha opiniao tecnica
A ideia e boa e melhora bastante onboarding e produtividade. Recomendacao para evitar friccao:
- mostrar o prompt apenas quando houver configuracao salvo;
- manter configuracao para resetar preferencias na tela de configuracoes.

## Criterios de Aceite
- Ao abrir ferramenta com configuracao salvo, modal aparece com lista correta.
- Selecionar configuracao carrega os campos automaticamente.
- Seguir sem configuracao abre ferramenta sem preload.
- Dica com checkbox aparece e respeita `Nao exibir novamente`.
- Preferencias persistem apos reiniciar aplicacao.

## Riscos e Atencoes
- Excesso de modais pode incomodar: manter opcao de desligar por ferramenta.
- Fluxo deve ser consistente para `Configuracao` e `Projeto`.
- Nao quebrar abertura direta das ferramentas existentes.

## Status
- Estado: Proposta
- Data: $ts


