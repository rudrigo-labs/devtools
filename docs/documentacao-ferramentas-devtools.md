# DevTools - Documentacao de Ferramentas

Status: ativo  
Data: 2026-03-13

## 1. Como ler este guia
Este documento explica cada ferramenta do DevTools em linguagem pratica:
1. Para que serve
2. Quando usar
3. Campos principais
4. Fluxo rapido de uso

## 2. Conceitos comuns (importante)
1. `Dry Run`: simulacao. Mostra o que seria feito, sem alterar arquivos/processos.
2. `Glob`: padrao para filtrar arquivos.  
Exemplos:
- `**/*.cs` = todos os arquivos `.cs` em qualquer pasta.
- `**/bin/**` = tudo dentro de pastas `bin`.
3. `Regex`: busca por expressao regular (padrao avancado).
4. `Case sensitive`: diferencia maiusculas e minusculas.
5. `Configurar manualmente`: executa sem usar configuracao salva.

## 3. Snapshot
### O que faz
Gera snapshot textual do projeto para analise com IA.

### Quando usar
Use quando precisar exportar uma visao consolidada de codigo e estrutura.

### Campos principais
1. Pasta do projeto
2. Pasta de saida
3. Filtros de diretorios e extensoes
4. Formatos de saida: texto, HTML, JSON

### Fluxo rapido
1. Defina pastas e filtros.
2. Escolha formatos.
3. Clique em `Executar`.

## 4. Rename
### O que faz
Renomeia textos em lote (identificadores e namespaces) no projeto.

### Quando usar
Use para refatoracao ampla de nomes.

### Campos principais
1. Texto antigo
2. Texto novo
3. Pasta raiz
4. Modo (geral ou namespace)
5. Filtros de extensao/diretorio
6. Dry run

### Fluxo rapido
1. Informe antigo/novo.
2. Escolha o modo.
3. Rode dry run.
4. Execute de fato.

## 5. Harvest
### O que faz
Minera codigo reutilizavel (helpers, utilitarios, metodos de extensao).

### Quando usar
Use para catalogar e extrair trechos com potencial de reaproveitamento.

### Campos principais
1. Pasta de origem
2. Pasta de destino
3. Extensoes permitidas/ignoradas
4. Diretorios ignorados
5. Score minimo
6. Copiar arquivos

### Fluxo rapido
1. Selecione origem e destino.
2. Ajuste filtros e score.
3. Execute.

## 6. Image Split
### O que faz
Detecta e recorta componentes de uma imagem automaticamente.

### Quando usar
Use para quebrar sprites/componentes visuais em arquivos separados.

### Campos principais
1. Arquivo de imagem
2. Pasta de saida
3. Nome base e extensao de saida
4. Indice inicial
5. Threshold de transparencia
6. Tamanho minimo (largura x altura)

### Fluxo rapido
1. Selecione imagem e pasta.
2. Ajuste parametros de deteccao.
3. Execute.

## 7. Search Text
### O que faz
Busca texto em arquivos com filtros e opcoes avancadas.

### Quando usar
Use para localizar ocorrencias de texto, padroes ou regex.

### Campos principais
1. Pasta raiz
2. Padrao de busca
3. Regex (opcional)
4. Case sensitive
5. Palavra inteira
6. Include/Exclude globs
7. Limites de tamanho e ocorrencias

### Fluxo rapido
1. Informe pasta e padrao.
2. Ative regex somente se necessario.
3. Ajuste globs.
4. Clique em `Buscar`.

## 8. Organizer
### O que faz
Classifica e organiza documentos por categoria.

### Quando usar
Use para arrumar lotes de arquivos em estrutura organizada.

### Campos principais
1. Pasta de entrada
2. Pasta de saida (opcional)
3. Score minimo
4. `Mover arquivos (aplicar)` para efetivar mudancas

### Fluxo rapido
1. Defina entrada/saida.
2. Ajuste score.
3. Rode sem aplicar para validar.
4. Marque `Mover arquivos` e execute novamente.

## 9. UTF-8 Convert
### O que faz
Converte arquivos de texto para UTF-8.

### Quando usar
Use para padronizar encoding do projeto.

### Campos principais
1. Pasta raiz
2. Include/Exclude globs
3. Recursivo
4. UTF-8 com BOM (opcional)
5. Criar backup
6. Dry run

### Fluxo rapido
1. Defina pasta e globs.
2. Rode dry run.
3. Ative backup se quiser seguranca extra.
4. Execute conversao.

## 10. Migrations
### O que faz
Executa operacoes de migration com `dotnet ef`.

### Quando usar
Use para criar migration (`AddMigration`) ou atualizar banco (`UpdateDatabase`).

### Campos principais
1. Pasta raiz do projeto
2. Startup project
3. DbContext completo
4. Projeto de migration para SQL Server/SQLite
5. Acao e provedor
6. Nome da migration (quando `AddMigration`)
7. Argumentos adicionais
8. Dry run

### Fluxo rapido
1. Salve configuracao com caminhos e DbContext.
2. Escolha acao/provedor.
3. Informe nome da migration se necessario.
4. Execute.

## 11. SSH Tunnel
### O que faz
Abre e fecha tunel SSH para redirecionamento de porta.

### Quando usar
Use para acessar servicos remotos por porta local (exemplo: banco interno).

### Campos principais
1. Host SSH e porta SSH
2. Usuario SSH
3. Chave privada (opcional)
4. StrictHostKeyChecking
5. Timeout
6. Bind local (host/porta)
7. Destino remoto (host/porta)

### Fluxo rapido
1. Configure host, usuario e mapeamento.
2. Salve configuracao.
3. Clique em `Iniciar tunel`.
4. Para finalizar, clique em `Parar tunel`.

## 12. Ngrok
### O que faz
Exposicao publica de porta local via tunel ngrok.

### Quando usar
Use para compartilhar localmente API/webhook temporario.

### Campos principais
1. AuthToken
2. Executavel ngrok (opcional)
3. URL da API ngrok local
4. Protocolo (`http`/`https`)
5. Porta local
6. Acoes: iniciar, listar, status, encerrar

### Fluxo rapido
1. Configure token e caminho do executavel.
2. Defina protocolo e porta.
3. Inicie tunel.
4. Copie URL publica na lista de tuneis.

## 13. Notes
### O que faz
Editor de notas com armazenamento local e opcao de sincronizacao com Google Drive.

### Quando usar
Use para notas tecnicas, checklists e rascunhos vinculados ao projeto.

### Campos principais
1. Pasta local de notas
2. Lista de notas e editor de conteudo
3. Extensao `.md` ou `.txt`
4. Backup ZIP (export/import)
5. Integracao Google Drive:
- credentials.json
- folder id
- pasta de token OAuth

### Fluxo rapido
1. Configure pasta local (e Drive se desejar).
2. Crie nova nota.
3. Edite e salve.
4. Use export/import para backup.

## 14. Dicas finais de uso
1. Sempre rode dry run quando houver alteracao em lote.
2. Salve configuracoes nomeadas para tarefas recorrentes.
3. Use globs para reduzir ruido e aumentar precisao.
4. Se uma ferramenta parecer complexa, comecar pelo menor escopo (uma pasta pequena) ajuda a validar antes.
