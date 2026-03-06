# Experiencia de Perfis por Ferramenta - $ts

## Objetivo
Definir um fluxo de experiencia para uso de perfis/projetos ao abrir ferramentas, reduzindo retrabalho de preenchimento manual e mantendo opcao de uso sem perfil.

## Escopo
- Ferramentas com suporte a perfil/projeto.
- Nao altera regras de execucao das ferramentas.
- Foco em UX de entrada (momento do clique no card da ferramenta).

## Proposta de Fluxo
1. Usuario clica no card da ferramenta.
2. Se existir perfil/projeto salvo para a ferramenta e o prompt estiver habilitado:
   - Abrir modal "Selecionar Perfil/Projeto".
   - Acoes:
     - `Selecionar e continuar`
     - `Seguir sem perfil`
3. Se nao existir perfil/projeto salvo:
   - Abrir ferramenta diretamente no modo sem perfil.

## Modal: Selecionar Perfil/Projeto
- Titulo: `Selecionar Perfil` ou `Selecionar Projeto` (conforme ferramenta).
- Conteudo:
  - Lista de perfis/projetos salvos.
  - Destaque para o padrao.
- Botoes:
  - `Selecionar e continuar`
  - `Seguir sem perfil`
  - `Cancelar`

## Fluxo de "Seguir sem perfil"
Ao escolher `Seguir sem perfil`:
- Exibir dica contextual (uma vez por ferramenta):
  - "Dica: com perfil/projeto voce preenche tudo mais rapido."
- Exibir checkbox: `Nao exibir novamente`.
- Persistir a preferencia quando marcado.

## Regras de Comportamento
- O prompt de selecao so aparece se:
  - a ferramenta suporta perfil/projeto;
  - existe pelo menos 1 perfil/projeto salvo;
  - preferencia de exibicao do prompt esta habilitada.
- Se usuario clicar `Selecionar e continuar` sem selecionar item:
  - validar inline e impedir continuar.
- `Nao exibir novamente` deve ser por ferramenta (nao global).

## Preferencias de UX (persistencia)
Sugestao de chaves por ferramenta:
- `ShowProfileSelectorOnToolOpen.{ToolId}` (default: true)
- `HideNoProfileTip.{ToolId}` (default: false)

## Minha opiniao tecnica
A ideia e boa e melhora bastante onboarding e produtividade. Recomendacao para evitar friccao:
- mostrar o prompt apenas quando houver perfil salvo;
- manter configuracao para resetar preferencias na tela de configuracoes.

## Criterios de Aceite
- Ao abrir ferramenta com perfil salvo, modal aparece com lista correta.
- Selecionar perfil carrega os campos automaticamente.
- Seguir sem perfil abre ferramenta sem preload.
- Dica com checkbox aparece e respeita `Nao exibir novamente`.
- Preferencias persistem apos reiniciar aplicacao.

## Riscos e Atencoes
- Excesso de modais pode incomodar: manter opcao de desligar por ferramenta.
- Fluxo deve ser consistente para `Perfil` e `Projeto`.
- Nao quebrar abertura direta das ferramentas existentes.

## Status
- Estado: Proposta
- Data: $ts
