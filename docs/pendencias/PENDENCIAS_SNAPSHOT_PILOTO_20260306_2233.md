# PENDENCIAS - Snapshot Piloto (Novo Padrao)

- Status: Aberto
- Responsavel: Equipe DevTools
- Criado em: 2026-03-06 22:33
- Atualizado em: 2026-03-07 03:04
- Relacionado a: `docs/definicoes/DEFINICAO_FLUXO_PADRAO_FERRAMENTAS_E_PERSISTENCIA_20260306_2233.md`

## Contexto
Executar uma ferramenta piloto (`Snapshot`) no novo fluxo canonico, sem tocar nas demais tools nesta etapa.

## Itens
- [x] Fase 0 - Congelar escopo do piloto (somente Snapshot).
- [x] Fase 1 - Definir contrato base canonico (`Id slug`, `Name`, `Description`, `IsActive`, datas).
- [x] Fase 2 - Bloqueador: concluir demanda de Core compartilhado (`docs/pendencias/PENDENCIAS_CORE_COMPARTILHADO_20260307_0104.md`).
- [x] Fase 3 - Ajustar modelos/repositorios da Snapshot ao contrato base.
- [x] Fase 4 - Garantir que host apenas chama a tool (sem regra de negocio na UI).
- [ ] Fase 5 - Validar persistencia: banco para dados de dominio e JSON para configuracao global.
- [ ] Fase 6 - Rodar build + testes e registrar evidencias.

## Criterios de Conclusao
- [ ] Snapshot funcional no novo padrao.
- [ ] Sem regressao da ferramenta.
- [x] Documentacao da fase atualizada.

## Encerramento
- Resultado:
- Evidencias:
