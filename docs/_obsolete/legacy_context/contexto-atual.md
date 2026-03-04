# Contexto atual - DevTools (resumo da task)

Status: concluido (CLI + docs). WPF pendente por limitacao do Linux.

## O que foi feito (alto nivel)

1. Reorganizacao fisica das pastas em `src/`:
   - `src/Core/DevTools.Core`
   - `src/Tools/DevTools.*`
   - `src/Cli/DevTools.Cli`

2. CLI novo com foco em UX:
   - Menu horizontal no topo (fixo), formato `1 - comando`.
   - Tela limpa por tool.
   - Prompts consistentes (sem parenteses, sem duplicacao de dois pontos).
   - Input com prefixo visual `[>]`.
   - Cores e layout mais confortaveis.
   - Barra de progresso com percentual quando disponivel e spinner quando nao.
   - Pos-execucao com escolha: voltar ao menu / repetir tool / sair.

3. Comandos CLI implementados (menu interativo):
   - Harvest, Snapshot, SearchText, Rename, Utf8Convert, Organizer,
     ImageSplit, Migrations, Ngrok, SSHTunnel, Notes.

4. Documentacao criada para todas as tools (em `docs/`).
   - Inclui guia geral: `docs/tools-overview.md`.
   - Cada ferramenta tem seu arquivo `docs/tool-*.md`.
   - Documentacao de Notes com placeholders de configuracao de email.

## Branch

- Branch ativa: `features/organizacao-cli`.
- Tudo implementado foi enviado para `dev` (confirmado via contains).

## Pontos importantes

- WPF nao implementado (limitacao Linux). Precisa ser feito no Windows.
- CLI e docs estao completos.
- GitGuard reclamou de dados sensiveis; exemplos foram trocados por placeholders.

## Principais commits (resumo)

- Reorganizacao das pastas Core/Tools/Cli.
- Implementacao da camada CLI com UX.
- Ajustes visuais (menu no topo, layout adaptativo, progresso).
- Documentacao das tools.
- Sanitizacao de placeholders em docs (Notes).

## Arquivos-chave

- `src/Cli/DevTools.Cli/Program.cs`
- `src/Cli/DevTools.Cli/App/CliApp.cs`
- `src/Cli/DevTools.Cli/Ui/CliConsole.cs`
- `src/Cli/DevTools.Cli/Ui/CliMenu.cs`
- `src/Cli/DevTools.Cli/Ui/CliInput.cs`
- `docs/tools-overview.md`
- `docs/tool-*.md`

