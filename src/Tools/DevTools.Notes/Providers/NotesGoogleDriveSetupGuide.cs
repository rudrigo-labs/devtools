namespace DevTools.Notes.Providers;

/// <summary>
/// Guia passo a passo exibido na tela de configuração do Notes
/// para orientar o usuário a criar as credenciais OAuth2 no Google Cloud.
/// </summary>
public static class NotesGoogleDriveSetupGuide
{
    public static string GetGuideText() => """
        ╔══════════════════════════════════════════════════════════════════╗
        ║          COMO CONFIGURAR O GOOGLE DRIVE NO DEVTOOLS NOTES       ║
        ╚══════════════════════════════════════════════════════════════════╝

        PASSO 1 — Acesse o Google Cloud Console
        ─────────────────────────────────────────
        Abra: https://console.cloud.google.com

        PASSO 2 — Crie ou selecione um projeto
        ─────────────────────────────────────────
        1. No menu superior, clique em "Selecionar projeto"
        2. Clique em "Novo Projeto"
        3. Dê um nome (ex: "DevTools Notes") e clique em "Criar"

        PASSO 3 — Ative a API do Google Drive
        ─────────────────────────────────────────
        1. No menu lateral, vá em "APIs e Serviços" → "Biblioteca"
        2. Pesquise por "Google Drive API"
        3. Clique em "Google Drive API" e depois em "Ativar"

        PASSO 4 — Configure a Tela de Consentimento OAuth
        ─────────────────────────────────────────
        1. Vá em "APIs e Serviços" → "Tela de consentimento OAuth"
        2. Selecione o tipo "Externo" e clique em "Criar"
        3. Preencha o nome do app (ex: "DevTools Notes")
        4. Informe seu e-mail no campo de suporte e desenvolvedor
        5. Clique em "Salvar e continuar" até o final
        6. Na seção "Usuários de teste", adicione o seu e-mail Google
        7. Clique em "Salvar e continuar"

        PASSO 5 — Crie as Credenciais OAuth2
        ─────────────────────────────────────────
        1. Vá em "APIs e Serviços" → "Credenciais"
        2. Clique em "+ Criar Credenciais" → "ID do cliente OAuth"
        3. Em "Tipo de aplicativo", selecione "App para computador"
        4. Dê um nome (ex: "DevTools Desktop") e clique em "Criar"
        5. Na janela que abrir, clique em "Baixar JSON"
        6. Salve o arquivo como "credentials.json" em local seguro

        PASSO 6 — Encontre o ID da Pasta no Google Drive
        ─────────────────────────────────────────
        1. Abra o Google Drive: https://drive.google.com
        2. Crie uma pasta para suas notas (ex: "DevTools Notes")
        3. Abra a pasta criada
        4. Copie o ID da URL — é a parte após "/folders/"
           Exemplo: https://drive.google.com/drive/folders/[ESTE_É_O_ID]

        PASSO 7 — Preencha a configuração no DevTools
        ─────────────────────────────────────────
        • Caminho do credentials.json → informe o caminho completo do arquivo baixado
        • ID da Pasta do Drive        → cole o ID copiado no Passo 6
        • Pasta do Token OAuth2       → escolha uma pasta local para salvar o token
                                        (ex: C:\Users\Você\AppData\Local\DevTools\oauth)

        PASSO 8 — Conecte ao Google Drive
        ─────────────────────────────────────────
        1. Clique em "Conectar ao Google Drive" na tela de configuração
        2. O browser será aberto para você autorizar o acesso
        3. Após autorizar, feche o browser — o DevTools está conectado!

        ⚠ IMPORTANTE:
        • O arquivo credentials.json é sensível — não compartilhe
        • O token OAuth2 salvo na pasta local permite acesso sem re-autenticar
        • Para desconectar, clique em "Desconectar Drive" na configuração
        """;
}
