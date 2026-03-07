# Architecture

Referencias principais de arquitetura:
- `docs/definicoes/DEFINICAO_ARQUITETURA_PERSISTENCIA_ORQUESTRACAO_20260306_1856.md` (fonte canonica atual)
- `docs/ativos/TechnicalDoc.md` (detalhamento tecnico complementar)

Resumo:
- WPF atua como camada de apresentacao/orquestracao.
- Cada ferramenta mantem dominio e regras na propria biblioteca em `src/Tools/`.
- Persistencia de negocio deve convergir para camada de infraestrutura dedicada, fora do host WPF.
