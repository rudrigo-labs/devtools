namespace DevTools.Core.Models;

/// <summary>
/// Modelo de configuração estática da aplicação.
/// Lido do appsettings.json na inicialização pelo Host.
/// Distribuído via DI para quem precisar dos valores.
/// </summary>
public sealed class AppSettings
{
    public FileToolsSettings FileTools { get; set; } = new();
    public HistorySettings History { get; set; } = new();
    public ToolVisibilitySettings ToolVisibility { get; set; } = new();
}

/// <summary>
/// Configurações globais para ferramentas que varrem arquivos.
/// MaxFileSizeKb: limite padrão aplicado na execução.
/// AbsoluteMaxFileSizeKb: teto máximo — validator rejeita qualquer valor acima disso.
/// </summary>
public sealed class FileToolsSettings
{
    public int MaxFileSizeKb { get; set; } = 500;
    public int AbsoluteMaxFileSizeKb { get; set; } = 10_000;
    public List<string> DefaultIncludeGlobs { get; set; } = ["**/*"];
    public List<string> DefaultExcludeGlobs { get; set; } =
    [
        "**/.git/**",
        "**/bin/**",
        "**/obj/**",
        "**/.vs/**",
        "**/.idea/**",
        "**/.vscode/**",
        "**/node_modules/**"
    ];
}

/// <summary>
/// Configurações globais do histórico de uso das ferramentas.
/// Enabled: quando false, o histórico deixa de ser exibido e de ser registrado.
/// </summary>
public sealed class HistorySettings
{
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Configurações globais de visibilidade das ferramentas no host.
/// DisabledTools: lista de tags de ferramentas que devem ficar ocultas.
/// </summary>
public sealed class ToolVisibilitySettings
{
    public List<string> DisabledTools { get; set; } = [];
}
