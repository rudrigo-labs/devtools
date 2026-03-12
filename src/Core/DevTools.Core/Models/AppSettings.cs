namespace DevTools.Core.Models;

/// <summary>
/// Modelo de configuração estática da aplicação.
/// Lido do appsettings.json na inicialização pelo Host.
/// Distribuído via DI para quem precisar dos valores.
/// </summary>
public sealed class AppSettings
{
    public FileToolsSettings FileTools { get; set; } = new();
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
}
