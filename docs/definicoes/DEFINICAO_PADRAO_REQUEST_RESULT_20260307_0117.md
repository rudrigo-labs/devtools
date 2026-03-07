# DEFINICAO - Padrao Request e Result

- Status: Em andamento
- Criado em: 2026-03-07 01:17
- Objetivo: Padronizar comunicacao entre Host e Tool.

## Fluxo padrao
`Host -> Request -> Tool.Engine.ExecuteAsync(request) -> Result -> Host -> UI`

## Request
1. Representa a entrada da operacao.
2. Conter apenas dados.
3. Nao conter regra de negocio.
4. Ser validado antes da execucao da regra da tool.

## Result
1. Representa a saida da operacao.
2. Deve ser estruturado e previsivel.
3. Deve retornar:
- estado de sucesso/falha
- mensagem principal
- dados da operacao (quando houver)
- erros de validacao (quando houver)

## Regras obrigatorias
1. Toda execucao de tool usa Request -> ExecuteAsync -> Result.
2. A UI nunca chama logica interna sem passar pela Engine.
3. Result deve permitir consumo em UI e testes sem ifs ad-hoc.
4. Result compartilhado base fica no Core; Result especifico da tool pode extender/compor.

## Beneficios
- desacoplamento UI/Tool
- consistencia nas respostas
- simplicidade para testes automatizados
- previsibilidade para novas ferramentas
