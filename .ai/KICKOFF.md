# KICKOFF - MODO EXECUTOR

Este e o primeiro arquivo que um agente de IA deve ler ao abrir o repositorio.

Procedimento:

1) Localizar AGENT_EXECUTION_RULES.md
2) Ler o arquivo completamente
3) Localizar DOCUMENTATION_RULES.md
4) Ler o arquivo completamente
5) Se existir PROJECT_RULES.md, carregar tambem

Se houver conflito entre regras, PROJECT_RULES.md tem prioridade sobre as regras gerais do agente.

Apos carregar as regras o agente deve:

- confirmar quais arquivos foram encontrados
- NAO executar nada automaticamente
- aguardar instrucoes do usuario
