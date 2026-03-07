# Manual do Usuario - DevTools (Detalhado)

Data de referencia: 2026-03-05

## 1. O que e o DevTools

DevTools e uma aplicacao desktop para centralizar tarefas tecnicas de rotina:

- manipulacao de arquivos
- automacao de busca e renomeacao
- suporte de infraestrutura (SSH e Ngrok)
- apoio a migrations
- gestao de notas locais com backup/sync opcional

## 2. Como abrir a aplicacao

Em desenvolvimento:

```powershell
dotnet run --project src/Presentation/DevTools.Presentation.Wpf/DevTools.Presentation.Wpf.csproj
```

## 3. Estrutura da interface

A janela principal possui tres abas:

1. Ferramentas
2. Execucoes (Jobs)
3. Configuracoes

## 3.1 Ferramentas

Mostra os cards para abrir cada ferramenta.

## 3.2 Execucoes (Jobs)

Mostra tarefas em execucao/finalizadas, status e logs resumidos.

## 3.3 Configuracoes

Centraliza configuracoes globais e configuracoes por ferramenta.

## 4. Fluxo recomendado de uso

1. Abra `Configuracoes` e ajuste caminhos/defaults.
2. Configure configuracoes das ferramentas que voce mais usa.
3. Execute ferramentas pela aba `Ferramentas`.
4. Monitore execucao na aba `Jobs`.

## 5. Ferramentas (passo a passo)

## 5.1 Notes

Objetivo:

- criar/editar notas locais em `.txt` ou `.md`

Fluxo:

1. Clique em `Nova Nota`.
2. Preencha titulo e conteudo.
3. Clique em `Salvar`.
4. A nota sera salva localmente.

Google Drive (opcional):

- se habilitado em configuracoes, apos salvar localmente pode sincronizar.

Backup:

- exporta/importa ZIP de notas.

## 5.2 Organizer

Objetivo:

- organizar arquivos por categoria/regras.

Fluxo:

1. Selecione pasta de entrada.
2. Selecione pasta de saida.
3. Ajuste simulacao/regras.
4. Execute.

## 5.3 Harvest

Objetivo:

- coletar arquivos conforme filtros e score.

Fluxo:

1. Defina origem e destino.
2. Ajuste score minimo e filtros.
3. Execute.

## 5.4 SearchText

Objetivo:

- buscar texto em lote.

Fluxo:

1. Defina pasta raiz.
2. Informe padrao de busca.
3. Ajuste include/exclude (opcional).
4. Execute e revise resultado.

## 5.5 Rename

Objetivo:

- renomeacao/refatoracao em lote.

Fluxo:

1. Defina pasta raiz.
2. Informe texto antigo e novo.
3. Ajuste include/exclude e dry-run.
4. Execute.

## 5.6 Snapshot

Objetivo:

- gerar snapshot estrutural de projeto.

Fluxo:

1. Selecione pasta do projeto.
2. Escolha formatos de saida.
3. Gere snapshot.

## 5.7 Utf8Convert

Objetivo:

- converter arquivos para UTF-8.

Fluxo:

1. Selecione pasta raiz.
2. Execute conversao.

## 5.8 Image Splitter

Objetivo:

- dividir imagem em partes menores.

Fluxo:

1. Selecione imagem de entrada.
2. Selecione pasta de saida.
3. Ajuste parametros.
4. Execute.

## 5.9 Migrations

Objetivo:

- facilitar comandos de migration EF Core.

Fluxo:

1. Defina root do projeto.
2. Defina startup project.
3. Informe DbContext.
4. Escolha acao.
5. Execute.

## 5.10 SSH Tunnel

Objetivo:

- abrir tunel SSH local/remoto.

Fluxo:

1. Informe host, porta, usuario e chave.
2. Informe mapeamento local/remoto.
3. Clique em conectar.
4. Ao terminar, clique em fechar tunel.

## 5.11 Ngrok

Objetivo:

- expor porta local com tunel publico.

Primeiro uso:

1. Se nao configurado, use onboarding.
2. Clique para criar conta no ngrok (se necessario).
3. Cole `Authtoken` e salve.

Uso diario:

1. Informe porta local.
2. Inicie tunel.
3. Copie URL publica.
4. Pare tunel quando terminar.

Importante:

- para teste real, `ngrok.exe` precisa estar instalado e acessivel.

## 5.12 Logs

- visualizar logs
- limpar logs quando necessario

## 6. Configuracoes (detalhamento pratico)

Painel `Configuracoes Gerais`:

- Armazenamento
- Harvest
- Organizer
- Migrations
- Ngrok
- Notes e Nuvem

Painel `Configuracoes de Ferramentas`:

- Rename
- Migrations
- Harvest
- SearchText
- Snapshot
- SSHTunnel

Regras:

- campos obrigatorios nao podem ficar vazios
- botoes de salvar aplicam validacao antes de persistir

## 7. Notas + Google Drive (regra oficial)

Regra:

1. sempre salva localmente primeiro
2. nuvem e opcional

Ou seja:

- sem Google Drive configurado, continua funcionando normal
- com Google Drive ativo, envia apos salvar local

## 8. Persistencia

- backend de configuracao: JSON (padrao) ou SQLite (opcional)
- notas: sempre arquivo fisico local

Detalhes completos:

- `docs/ativos/CONFIGURACOES_FERRAMENTAS_DETALHADO.md`

## 9. Build/Testes/Instalador

Build:

```powershell
dotnet build src/DevTools.slnx -c Debug
```

Testes:

```powershell
dotnet test src/Tools/DevTools.Tests/DevTools.Tests.csproj -v minimal
```

Instalador:

```powershell
build\build_installer.bat 1.0.0
```

## 10. Solucao rapida de problemas

### 10.1 Campo obrigatorio bloqueando salvar

- revise mensagem inline e preencha os campos listados.

### 10.2 Ngrok nao inicia

- confirme instalacao do ngrok
- confirme token salvo
- confirme porta valida

### 10.3 Google Drive nao conecta

- revise `ClientId`, `ClientSecret`, `ProjectId`, `FolderName`
- use `Testar Conexao`

### 10.4 Caminhos nao persistem

- valide permissao em `%AppData%\DevTools`
- valide backend selecionado (JSON/SQLite)

## 11. Estado atual de validacao

- suite automatizada: 36 aprovados, 2 ignorados, 0 falhas
- ha 2 cenarios WPF em `Skip` por thread affinity do host de testes

Checklist manual desses cenarios:

- `docs/TESTES_MANUAIS_PENDENTES_2026-03-05.md`

