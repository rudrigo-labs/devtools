# Guia de Configuração (appsettings.json)

O DevTools utiliza um arquivo de configuração centralizado chamado `appsettings.json`, localizado no diretório raiz da aplicação (onde o executável `DevTools.Presentation.Wpf.exe` está).

Este arquivo controla as configurações de persistência para ferramentas como o Túnel SSH, configurações de envio de e-mail (para ferramentas futuras) e strings de conexão.

## Localização do Arquivo

- **Caminho:** `./appsettings.json` (na mesma pasta do executável)
- **Formato:** JSON

Se o arquivo não existir, o aplicativo criará um automaticamente com valores padrão na primeira execução.

## Estrutura das Configurações

### 1. Seção SSH (`Ssh`)
Armazena os perfis de conexão para o Túnel SSH.

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `Profiles` | Lista | Lista de objetos contendo os perfis de túnel. |
| `Name` | Texto | Nome de exibição do perfil na lista. |
| `SshHost` | Texto | Endereço do servidor SSH (IP ou Hostname). |
| `SshPort` | Inteiro | Porta do servidor SSH (Padrão: 22). |
| `SshUser` | Texto | Usuário para autenticação SSH. |
| `IdentityFile` | Texto | Caminho absoluto para o arquivo da chave privada (.pem, .ppk, .key). |
| `LocalBindHost` | Texto | Interface local para bind (geralmente "127.0.0.1"). |
| `LocalPort` | Inteiro | Porta local que será aberta na sua máquina. |
| `RemoteHost` | Texto | Destino dentro da rede remota (ex: servidor de banco de dados). |
| `RemotePort` | Inteiro | Porta do serviço no destino remoto. |
| `StrictHostKeyChecking`| Texto | Validação de host key ("Default", "Yes", "No", "Ask"). |

### 2. Seção Email (`Email`)
Configurações para ferramentas que requerem envio de e-mail (ex: notificações).

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `SmtpHost` | Texto | Endereço do servidor SMTP. |
| `SmtpPort` | Inteiro | Porta do servidor SMTP (ex: 587). |
| `Username` | Texto | Usuário para autenticação SMTP. |
| `Password` | Texto | Senha do usuário SMTP. |

### 3. Seção ConnectionStrings (`ConnectionStrings`)
Dicionário para armazenar strings de conexão de banco de dados usadas por ferramentas como Migrations Helper.

---

## Exemplo de Arquivo de Configuração

Abaixo está um exemplo completo de como o arquivo `appsettings.json` deve se parecer:

```json
{
  "ConnectionStrings": {
    "MyDatabase": "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;"
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "Username": "meu.email@gmail.com",
    "Password": "minhasenhasupersegura"
  },
  "Ssh": {
    "Profiles": [
      {
        "Name": "Banco de Produção",
        "SshHost": "192.168.1.100",
        "SshPort": 22,
        "SshUser": "admin",
        "IdentityFile": "C:\\Users\\Rodrigo\\.ssh\\id_rsa",
        "LocalBindHost": "127.0.0.1",
        "LocalPort": 1433,
        "RemoteHost": "db-prod-internal",
        "RemotePort": 1433,
        "StrictHostKeyChecking": "No",
        "ConnectTimeoutSeconds": 30
      },
      {
        "Name": "Redis Staging",
        "SshHost": "staging.empresa.com",
        "SshPort": 2222,
        "SshUser": "devops",
        "IdentityFile": "",
        "LocalBindHost": "127.0.0.1",
        "LocalPort": 6379,
        "RemoteHost": "localhost",
        "RemotePort": 6379,
        "StrictHostKeyChecking": "Default",
        "ConnectTimeoutSeconds": null
      }
    ]
  }
}
```
