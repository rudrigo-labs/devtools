# Regras de ExecuÃ§Ã£o e Contexto - 20260306_0225

## Objetivo
Evitar perda de contexto e regressÃµes durante ajustes de UI/fluxo.

## Regras de ExecuÃ§Ã£o
- Executar uma demanda por vez, sem ampliar escopo.
- Antes de editar, confirmar em linguagem simples o que serÃ¡ alterado.
- NÃ£o criar fluxo novo quando jÃ¡ existe fluxo padrÃ£o pronto no projeto.
- Em ajustes visuais, reaproveitar padrÃµes jÃ¡ existentes.
- ApÃ³s cada ajuste, validar build antes de seguir para o prÃ³ximo item.

## Regra de Configuracao/Projeto (PadrÃ£o)
- Ao abrir a tela de configuracoes/projetos:
  - Se nÃ£o existir item, criar automaticamente o primeiro (`Configuracao1` ou `Projeto1`).
  - Entrar direto no item criado (modo ediÃ§Ã£o).
  - BotÃ£o `Novo` deve ficar desabilitado enquanto o item novo nÃ£o for salvo.
- O primeiro item salvo Ã© obrigatoriamente o padrÃ£o.
- Ao abrir a tela novamente, selecionar automaticamente o padrÃ£o.
- A lista da esquerda Ã© a fonte de navegaÃ§Ã£o principal e deve refletir o item ativo.

## Regra EspecÃ­fica para EF Core Migrations
- Mesma lÃ³gica de configuracoes, com nomenclatura de projeto:
  - `Projeto1`, `Projeto2`, etc.
- NÃ£o usar nomenclatura mista em tela (evitar alternÃ¢ncia entre Configuracao/Projeto no mesmo contexto).

## Regra de Layout para ConfiguraÃ§Ã£o de Migrations
- Sem barra de rolagem no painel de Migrations.
- No bloco `ConfiguraÃ§Ã£o do Contexto`:
  - `DbContext` e `Argumentos` lado a lado.
  - Tags de argumentos abaixo do campo `Argumentos`.
- Evitar corte visual dos controles; ajustar distribuiÃ§Ã£o em colunas.

