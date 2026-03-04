KICKOFF — EXECUTOR MODE

Embora o diretório raiz aberto no workspace contenha todo o repositório, os projetos reais da solução estão obrigatoriamente dentro da pasta src/.

Interpretação obrigatória:

A pasta src/ deve ser considerada a raiz lógica da solução.

Nenhum projeto deve ser criado na raiz do repositório.

Nenhum arquivo de código deve ser criado fora de src/.

Toda análise, criação, modificação ou geração de artefatos deve ocorrer dentro de src/.

É proibido:

Gerar .csproj na raiz.

Criar estrutura paralela a src/.

Assumir que a raiz do workspace é a raiz da solução.

Mover projetos para fora de src/.

Essa regra é estrutural e permanente e deve ser respeitada em qualquer janela, sessão ou contexto.

Antes de qualquer ação:
1) Procure na raiz do repositório o arquivo AGENT_EXECUTION_RULES.md
2) Se NÃO encontrar, responda apenas:
"Arquivo AGENT_EXECUTION_RULES.md não encontrado. Vou aguardar você adicioná-lo."
3) Se encontrar, leia e siga estritamente.


Regras adicionais:
- Se existir um arquivo AGENT_DEVTOOLS_RULES.md, leia-o também.
- Caso exista, considere-o uma EXTENSÃO das regras base.
- Em caso de conflito, as regras do projeto vencem.


Agora:
- NÃO execute nada.
- NÃO proponha arquitetura.
- Apenas confirme quais arquivos de regras foram encontrados
e aguarde minhas instruções.