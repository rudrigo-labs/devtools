# DevTools.Notes - Como usar (CLI)

## Objetivo

Ler, salvar e enviar notas por e-mail.

## Acoes

- Ler nota
- Salvar nota
- Enviar por e-mail

## Entradas

- Nome da nota (obrigatorio para ler/salvar)
- Pasta das notas (opcional)
- Conteudo da nota (obrigatorio para salvar)
- Sobrescrever (s/n)
- Config de e-mail (opcional)

## Configuracao de e-mail

Por padrao, o arquivo fica em:

- Windows: `%LOCALAPPDATA%\DevTools\mail-config.json`

Exemplo de JSON (placeholders):

{
  "smtpHost": "<SMTP_HOST>",
  "smtpPort": 587,
  "username": "<SMTP_USER>",
  "password": "<SMTP_PASSWORD>",
  "enableSsl": true,
  "fromEmail": "<FROM_EMAIL>",
  "toEmail": "<TO_EMAIL>"
}

Observacoes:

- Nao comite credenciais reais.
- Voce pode usar variaveis de ambiente no arquivo, por exemplo: "password": "${SMTP_PASSWORD}".

## Saida

- Dados da nota lida/salva
- Resultado do envio de e-mail
