# DEFINICAO - Alinhamento de Infraestrutura (Hosts sem CLI)

- Status: Em andamento
- Criado em: 2026-03-07 00:31
- Tema: Consistencia entre infraestrutura de projetos, solution e documentacao

## Objetivo
Alinhar a estrutura de projetos com a arquitetura definida:
- remover CLI da fase atual;
- consolidar host em `src/Hosts/DevTools.Host.Wpf`;
- garantir consistencia entre `src/DevTools.slnx` e `docs/architecture.md`.

## Escopo
1. Solution referenciar o host correto.
2. Documentacao refletir a infraestrutura real.
3. Rastrear mudancas no log de demanda.
