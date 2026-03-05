namespace DevTools.Ngrok.Services;

public sealed class NgrokOnboardingService
{
    public const string SignupUrl = "https://dashboard.ngrok.com/signup";
    public const string TokenPageUrl = "https://dashboard.ngrok.com/get-started/your-authtoken";

    public string GetSignupUrl() => SignupUrl;

    public string GetTokenPageUrl() => TokenPageUrl;

    public IReadOnlyList<string> GetSetupSteps()
    {
        return
        [
            "Crie uma conta gratuita no ngrok.",
            "Acesse o painel da sua conta.",
            "Copie seu Authtoken.",
            "Cole o token na tela de configuracao do DevTools."
        ];
    }
}
