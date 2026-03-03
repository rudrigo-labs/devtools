using System.Windows.Documents;
using System.Windows.Media;
using System.Windows;

namespace DevTools.Presentation.Wpf.Services
{
    public static class HelpContentProvider
    {
        public static FlowDocument GetGoogleDriveHelp()
        {
            var document = new FlowDocument();

            // Estilos
            var titleStyle = new Style(typeof(Paragraph));
            titleStyle.Setters.Add(new Setter(Paragraph.FontSizeProperty, 24.0));
            titleStyle.Setters.Add(new Setter(Paragraph.FontWeightProperty, FontWeights.Bold));
            titleStyle.Setters.Add(new Setter(Paragraph.ForegroundProperty, System.Windows.Media.Brushes.White));
            titleStyle.Setters.Add(new Setter(Paragraph.MarginProperty, new Thickness(0, 0, 0, 20)));

            var h2Style = new Style(typeof(Paragraph));
            h2Style.Setters.Add(new Setter(Paragraph.FontSizeProperty, 20.0));
            h2Style.Setters.Add(new Setter(Paragraph.FontWeightProperty, FontWeights.SemiBold));
            h2Style.Setters.Add(new Setter(Paragraph.ForegroundProperty, (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("AccentBrush")));
            h2Style.Setters.Add(new Setter(Paragraph.MarginProperty, new Thickness(0, 15, 0, 10)));

            var bodyStyle = new Style(typeof(Paragraph));
            bodyStyle.Setters.Add(new Setter(Paragraph.FontSizeProperty, 14.0));
            bodyStyle.Setters.Add(new Setter(Paragraph.LineHeightProperty, 22.0));
            bodyStyle.Setters.Add(new Setter(Paragraph.ForegroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 220, 220))));
            bodyStyle.Setters.Add(new Setter(Paragraph.MarginProperty, new Thickness(0, 0, 0, 8)));

            var codeBlockStyle = new Style(typeof(Paragraph));
            codeBlockStyle.Setters.Add(new Setter(Paragraph.FontFamilyProperty, new System.Windows.Media.FontFamily("Consolas")));
            codeBlockStyle.Setters.Add(new Setter(Paragraph.FontSizeProperty, 13.0));
            codeBlockStyle.Setters.Add(new Setter(Paragraph.BackgroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30))));
            codeBlockStyle.Setters.Add(new Setter(Paragraph.ForegroundProperty, (System.Windows.Media.Brush)System.Windows.Application.Current.FindResource("AccentBrush")));
            codeBlockStyle.Setters.Add(new Setter(Paragraph.PaddingProperty, new Thickness(12)));
            codeBlockStyle.Setters.Add(new Setter(Paragraph.MarginProperty, new Thickness(0, 10, 0, 15)));
            codeBlockStyle.Setters.Add(new Setter(Paragraph.BorderBrushProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(63, 63, 70))));
            codeBlockStyle.Setters.Add(new Setter(Paragraph.BorderThicknessProperty, new Thickness(1)));

            // Adicionando conteúdo
            document.Blocks.Add(new Paragraph(new Run("Guia de Configuração: Ativando seu Backup Pessoal no Google Drive")) { Style = titleStyle });
            document.Blocks.Add(new Paragraph(new Run("Este guia orienta você a criar sua própria \"Chave de Comunicação\" com o Google. Ao fazer isso, suas notas serão enviadas do seu computador diretamente para o seu Google Drive, sem passar por servidores de terceiros. 100% privado e seguro.")) { Style = bodyStyle });
            
            document.Blocks.Add(new Paragraph(new Run("Informações Importantes sobre Custos e Privacidade")) { Style = h2Style });
            document.Blocks.Add(new Paragraph(new Run("Custo Zero: O uso da API do Google Drive é gratuito para uso pessoal. Você não precisa inserir cartões de crédito.")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("Espaço de Armazenamento: Seus backups usarão os 15GB gratuitos que o Google oferece em toda conta Gmail.")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("Privacidade: Como você criará sua própria chave, ninguém (nem o desenvolvedor deste software) terá acesso aos seus arquivos ou senhas.")) { Style = bodyStyle });

            document.Blocks.Add(new Paragraph(new Run("Passo 1: Criar seu Projeto no Google Cloud")) { Style = h2Style });
            document.Blocks.Add(new Paragraph(new Run("1. Acesse o https://console.cloud.google.com/")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("2. Entre com sua conta do Gmail.")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("3. No topo da página, clique em \"Selecionar um projeto\" (ou no nome do projeto atual).")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("4. Escolha \"NOVO PROJETO\".")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("5. Nomeie como \"Meu Backup de Notas\".")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("6. Clique em \"CRIAR\" e aguarde alguns segundos até finalizar.")) { Style = bodyStyle });

            document.Blocks.Add(new Paragraph(new Run("Passo 2: Ativar a API do Google Drive")) { Style = h2Style });
            document.Blocks.Add(new Paragraph(new Run("1. No menu lateral (três barras no canto superior esquerdo), vá em:")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("APIs e Serviços > Biblioteca")) { Style = codeBlockStyle });
            document.Blocks.Add(new Paragraph(new Run("2. Na barra de busca, digite:")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("Google Drive API")) { Style = codeBlockStyle });
            document.Blocks.Add(new Paragraph(new Run("3. Clique no ícone do Google Drive.")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("4. Clique no botão azul ATIVAR.")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("🔎 Acesso direto à API: https://console.cloud.google.com/apis/library/drive.googleapis.com")) { Style = bodyStyle });

            document.Blocks.Add(new Paragraph(new Run("Passo 3: Configurar seu Acesso Pessoal (Vital)")) { Style = h2Style });
            document.Blocks.Add(new Paragraph(new Run("Como o projeto é seu, você precisa se dar permissão de uso.")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("1. No menu lateral, clique em \"Tela de consentimento OAuth\".")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("2. Escolha a opção \"Externo\".")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("3. Clique em \"CRIAR\".")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("4. Preencha apenas os campos obrigatórios:")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("Nome do app: Meu Backup")) { Style = codeBlockStyle });
            document.Blocks.Add(new Paragraph(new Run("E-mail de suporte: Seu próprio e-mail")) { Style = codeBlockStyle });
            document.Blocks.Add(new Paragraph(new Run("Dados de contato do desenvolvedor: Seu próprio e-mail novamente")) { Style = codeBlockStyle });
            document.Blocks.Add(new Paragraph(new Run("5. Clique em \"SALVAR E CONTINUAR\" até chegar na aba \"Usuários de teste\".")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("🚨 PASSO CRUCIAL")) { Style = h2Style });
            document.Blocks.Add(new Paragraph(new Run("1. Clique em \"+ ADD USERS\".")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("2. Digite o seu e-mail do Gmail.")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("3. Clique em \"Adicionar\" e depois em \"Salvar\".")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("Isso informa ao Google que você é a única pessoa autorizada a usar esta chave.")) { Style = bodyStyle });

            document.Blocks.Add(new Paragraph(new Run("Passo 4: Gerar os Códigos de Configuração")) { Style = h2Style });
            document.Blocks.Add(new Paragraph(new Run("1. No menu lateral, clique em \"Credenciais\".")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("2. No topo, clique em:")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("+ CRIAR CREDENCIAIS > ID do cliente OAuth")) { Style = codeBlockStyle });
            document.Blocks.Add(new Paragraph(new Run("3. Em \"Tipo de aplicativo\", selecione:")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("App de desktop")) { Style = codeBlockStyle });
            document.Blocks.Add(new Paragraph(new Run("4. Clique em \"CRIAR\".")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("Uma janela será exibida com seus códigos.")) { Style = bodyStyle });

            document.Blocks.Add(new Paragraph(new Run("Passo 5: Ativando no Aplicativo")) { Style = h2Style });
            document.Blocks.Add(new Paragraph(new Run("Abra a janela de Configurações do sistema e copie os seguintes dados exibidos na tela do Google:")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("Project ID: (Ex: meu-backup-notas-45621)")) { Style = codeBlockStyle });
            document.Blocks.Add(new Paragraph(new Run("Client ID: (Sequência longa terminando em .apps.googleusercontent.com)")) { Style = codeBlockStyle });
            document.Blocks.Add(new Paragraph(new Run("Client Secret: (Sua chave secreta de segurança)")) { Style = codeBlockStyle });
            document.Blocks.Add(new Paragraph(new Run("Clique em \"Salvar e Testar Conexão\".")) { Style = bodyStyle });

            document.Blocks.Add(new Paragraph(new Run("O que esperar no primeiro uso?")) { Style = h2Style });
            document.Blocks.Add(new Paragraph(new Run("Na primeira vez que você salvar uma nota:")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("1. Seu navegador abrirá automaticamente.")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("2. Faça login com sua conta do Google.")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("3. O Google exibirá um aviso:")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("O Google não verificou este app")) { Style = codeBlockStyle });
            document.Blocks.Add(new Paragraph(new Run("Isso é normal, pois a chave foi criada por você agora.")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("1. Clique em \"Avançado\".")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("2. Clique no link no rodapé: \"Ir para [Nome do seu Projeto] (inseguro)\".")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("3. Clique em \"Continuar\" para autorizar o envio dos arquivos.")) { Style = bodyStyle });

            document.Blocks.Add(new Paragraph(new Run("Links Oficiais")) { Style = h2Style });
            document.Blocks.Add(new Paragraph(new Run("Console Google Cloud: https://console.cloud.google.com/")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("Biblioteca de APIs: https://console.cloud.google.com/apis/library")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("Google Drive API: https://console.cloud.google.com/apis/library/drive.googleapis.com")) { Style = bodyStyle });
            document.Blocks.Add(new Paragraph(new Run("Documentação Oficial Drive API: https://developers.google.com/drive")) { Style = bodyStyle });

            document.Blocks.Add(new Paragraph(new Run("Pronto! Suas notas agora estão protegidas na sua nuvem particular.")) { Style = bodyStyle });

            return document;
        }
    }
}