# DevTools - Pendencias UI e Ferramentas

Data: 2026-03-05
Status: Aberto

## 1) Tooltip com estilo do tema

- Pendencia: Padronizar visual dos ToolTips para nao parecer tooltip nativo do Windows.
- Problema atual: Os baloes de tooltip estao com aparencia fora do tema IDE Style.
- Objetivo:
  - Fundo, borda, fonte e espaco interno seguindo o tema atual.
  - Melhor contraste e legibilidade.
  - Comportamento consistente em todas as janelas.
- Criterio de aceite:
  - Tooltip com estilo unico do DevTools em toda a aplicacao.
  - Sem regressao visual em hover dos componentes.

## 2) Harvest: padroes de pastas ignoradas

- Pendencia: Preencher por padrao a lista de pastas ignoradas do Harvest na tela de configuracao.
- Problema atual: O usuario pode abrir a configuracao sem uma base inicial util para exclusao de pastas tecnicas comuns.
- Objetivo:
  - Inserir defaults editaveis na caixa "Pastas Ignoradas" (ex.: `bin`, `obj`, `.vs`, `.git`, `node_modules`).
  - Manter liberdade total para o usuario remover/adicionar itens.
  - Garantir consistencia entre backend JSON e SQLite.
- Criterio de aceite:
  - Ao abrir a configuracao do Harvest sem valores salvos, a lista vem preenchida com padroes uteis.
  - O usuario consegue alterar e salvar normalmente.
  - O valor salvo prevalece sobre o padrao na proxima abertura.

## 3) Observacao de execucao

- Regra combinada: fechar primeiro as pendencias ativas antes de abrir novas frentes grandes.
- Proxima revisao deste arquivo: apos fechar os ajustes de configuracoes e validacoes.

## 4) Notes: checkbox do Google Drive inicia marcado

- Pendencia: Ajustar estado inicial do checkbox "Habilitar Backup no Google Drive" na configuracao de Notas e Nuvem.
- Problema atual: O checkbox esta abrindo marcado por padrao, mesmo sem acao do usuario.
- Objetivo:
  - Abrir sempre desmarcado por padrao quando nao houver configuracao salva.
  - Respeitar o valor persistido quando existir configuracao previa.
- Criterio de aceite:
  - Primeira abertura da tela: checkbox desmarcado.
  - Apos salvar configuracao: checkbox reflete exatamente o valor salvo.
