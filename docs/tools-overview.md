# DevTools - Guia Geral de Uso (CLI)

Status: Em uso

## Como executar

1. Execute o CLI:
   - Windows: `DevTools.Cli.exe`
   - Ou via dotnet: `dotnet DevTools.Cli.dll`
2. O menu principal aparece.
3. Escolha a ferramenta pelo numero ou pelo comando.
4. Preencha os dados solicitados na tela.

## Padroes do CLI

- O fluxo e interativo. Os prompts pedem as entradas necessarias.
- Para cancelar a qualquer momento, digite `c`.
- Apos concluir, voce pode voltar ao menu, repetir a ferramenta ou sair.
- Existe modo compacto no menu (opcao 99).

## Ferramentas disponiveis

- Harvest
- Snapshot
- SearchText
- Rename
- Utf8 Convert
- Organizer
- Image Split
- Migrations
- Ngrok
- SSHTunnel
- Notes

Cada ferramenta possui um documento proprio em `docs/`.

## Nota sobre configuracoes

- Sempre use placeholders nos exemplos de configuracao.
- Nunca comite credenciais reais.
