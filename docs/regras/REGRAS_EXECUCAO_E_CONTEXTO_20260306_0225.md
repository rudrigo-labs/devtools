# Regras de Execução e Contexto - 20260306_0225

## Objetivo
Evitar perda de contexto e regressões durante ajustes de UI/fluxo.

## Regras de Execução
- Executar uma demanda por vez, sem ampliar escopo.
- Antes de editar, confirmar em linguagem simples o que será alterado.
- Não criar fluxo novo quando já existe fluxo padrão pronto no projeto.
- Em ajustes visuais, reaproveitar padrões já existentes.
- Após cada ajuste, validar build antes de seguir para o próximo item.

## Regra de Perfil/Projeto (Padrão)
- Ao abrir a tela de perfis/projetos:
  - Se não existir item, criar automaticamente o primeiro (`Perfil1` ou `Projeto1`).
  - Entrar direto no item criado (modo edição).
  - Botão `Novo` deve ficar desabilitado enquanto o item novo não for salvo.
- O primeiro item salvo é obrigatoriamente o padrão.
- Ao abrir a tela novamente, selecionar automaticamente o padrão.
- A lista da esquerda é a fonte de navegação principal e deve refletir o item ativo.

## Regra Específica para EF Core Migrations
- Mesma lógica de perfis, com nomenclatura de projeto:
  - `Projeto1`, `Projeto2`, etc.
- Não usar nomenclatura mista em tela (evitar alternância entre Perfil/Projeto no mesmo contexto).

## Regra de Layout para Configuração de Migrations
- Sem barra de rolagem no painel de Migrations.
- No bloco `Configuração do Contexto`:
  - `DbContext` e `Argumentos` lado a lado.
  - Tags de argumentos abaixo do campo `Argumentos`.
- Evitar corte visual dos controles; ajustar distribuição em colunas.
