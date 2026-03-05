# Planejamento de Evolução das Configurações (Roadmap de UX e Funcionalidades)

Este documento detalha as ideias e conceitos discutidos para a modernização das telas de configuração do DevTools Hub. Caso os créditos do assistente terminem, este roadmap serve como guia para as próximas implementações.

## 1. Visão Geral: Dashboard de Configuração Ativa
A ideia é transformar a aba de configurações de uma lista estática de campos em um painel informativo e "vivo".

### Ideias de Status em Tempo Real (Cards Informativos):
- **SSH**: Exibir "X perfis configurados | Último usado: [Nome]".
- **Ngrok**: Exibir status do serviço ("Online/Offline") e a porta ativa.
- **Harvest**: Exibir um resumo dos filtros ativos (ex: ".cs, .ts").

## 2. Funcionalidades Inteligentes (Smart Settings)
Adicionar automações que facilitem o preenchimento dos campos:

- **Detecção de DbContext (Migrations)**: Um botão de "Varinha Mágica" ao lado do campo que faz um scan no projeto selecionado e preenche automaticamente o nome completo do DbContext encontrado.
- **Localizador de Executáveis (Ngrok/SSH)**: Botão para buscar automaticamente o `ngrok.exe` ou `ssh.exe` em pastas comuns do sistema (Program Files, AppData, etc.).
- **Sugestão de Nomes (Snapshot)**: Gerar automaticamente o nome do arquivo ZIP baseado no nome da pasta raiz e na data atual.

## 3. Refinamento de UX (Interface do Usuário)
Substituir controles básicos por elementos mais modernos e intuitivos:

- **Toggle Switches**: Usar chaves de ligar/desligar em vez de CheckBoxes para opções binárias (ex: "IA Ativada", "Dry Run").
- **Chips/Tags (Harvest)**: Para extensões de arquivo e pastas ignoradas, usar "Chips" (etiquetas) que podem ser adicionadas ou removidas com um "x", em vez de uma string separada por vírgula.
- **Validação Visual**: Campos com erro (ex: caminho que não existe) devem ganhar uma borda vermelha suave e um ícone de aviso em tempo real.

## 4. Seção de "Health Check"
Um pequeno painel no topo de cada ferramenta de configuração indicando se ela está pronta para uso:
- ✅ Projeto configurado corretamente.
- ⚠️ Token do Ngrok não informado (opcional).
- ❌ Chave SSH não encontrada.

## 5. Padronização Restante
Aplicar o modelo visual utilizado no **Túnel SSH** (Cards Claros, Layout em Colunas e Floating Hints) nos painéis que ainda utilizam o visual antigo:
- **Painel de Configurações Gerais**.
- **Painel do Organizador (Refinar para o novo padrão)**.
- **Painel do Ngrok (Refinar para o novo padrão)**.
