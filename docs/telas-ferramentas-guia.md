# DevTools - Guia das Telas de Ferramentas

Status: ativo  
Base: padrao canonico (ToolHeader -> ActionBar -> ToolBody -> Status)

## Objetivo
Este documento explica, de forma pratica, como usar cada tela de ferramenta do DevTools:
- para que serve
- quais campos sao obrigatorios
- como executar
- quando usar modo de configuracao

## Padrao visual usado em todas as tools
- Topo com titulo e subtitulo da ferramenta.
- ActionBar com acoes (Novo/Salvar/Executar/Excluir/Cancelar).
- Icone de ajuda na ActionBar com resumo rapido de uso.
- Corpo com campos e explicacoes.
- Status no final da tela.

## 1) Snapshot
- Serve para gerar snapshot textual do projeto (txt/html/json).
- Campos principais: pasta do projeto, pasta de saida, filtros de diretorio/extensao.
- Modo configuracao: nome e descricao ficam no topo da tela para salvar perfil.
- Acao principal: `Executar`.

## 2) Harvest
- Serve para minerar codigo reutilizavel.
- Campos principais: origem, destino, filtros.
- Modo configuracao: usar quando quiser salvar um perfil reutilizavel.
- Acao principal: `Executar`.

## 3) Image Split
- Serve para recortar componentes de uma imagem automaticamente.
- Campos principais: arquivo de entrada, pasta de saida, parametros de deteccao.
- Acao principal: `Executar`.

## 4) Rename
- Serve para renomeacao em lote (identificadores/namespaces).
- Campos principais: texto antigo, texto novo, raiz do projeto, filtros.
- Modos: geral ou somente namespace.
- Acao principal: `Executar` (usar simulacao antes de aplicar).

## 5) Notes
- Serve para criar e editar notas locais (com opcao de sincronizar Drive).
- Fluxo principal: lista de notas -> editor de nota.
- Configuracao unica: pasta local + campos do Google Drive.
- Editor inicia no canto superior esquerdo, com espaco interno de editor de texto.

## 6) Search Text
- Serve para buscar texto em arquivos.
- Campos principais: pasta raiz, padrao de busca.
- Opcoes: regex, case sensitive, palavra inteira, globs de inclusao/exclusao.
- Acao principal visivel na tela: `Buscar Agora`.

## 7) Organizer
- Serve para classificar e organizar arquivos por categoria.
- Campos principais: pasta de entrada, pasta de saida (opcional), score minimo.
- Opcao `Mover arquivos (aplicar)`: quando desligado, roda em simulacao.
- Acao principal visivel na tela: `Organizar Agora`.

## 8) UTF-8 Convert
- Serve para converter arquivos de texto para UTF-8.
- Campos principais: pasta raiz, globs de inclusao/exclusao.
- Opcoes: subpastas, BOM, backup, dry run.
- Acao principal visivel na tela: `Converter Agora`.

## 9) Migrations
- Serve para executar `dotnet ef` (AddMigration / UpdateDatabase).
- Modo execucao: escolher acao e provedor.
- Modo configuracao: salvar root/startup/dbcontext/projetos de migration.
- Acao principal visivel na tela: `Executar Agora`.

## 10) SSH Tunnel
- Serve para iniciar/parar tunel SSH.
- Modo execucao: iniciar/parar e ver status do tunel.
- Modo configuracao: salvar host, usuario, chaves e mapeamento local/remoto.
- Acao principal no modo execucao: `Iniciar tunel`.

## 11) Ngrok
- Serve para expor porta local com URL publica.
- Modo execucao: escolher protocolo/porta, iniciar e copiar URL.
- Modo configuracao: token, executavel, argumentos e URL da API.
- Acoes principais: `Iniciar`, `Parar`, `Atualizar`, `Status`.

## Regras praticas para novas melhorias
- Sempre deixar a acao principal visivel no corpo da tela quando fizer sentido.
- Sempre explicar campos tecnicos com texto curto e direto.
- Sempre deixar claro quando a tela esta em modo configuracao.
- Evitar espacamento excessivo no topo entre titulo e primeiro campo.

