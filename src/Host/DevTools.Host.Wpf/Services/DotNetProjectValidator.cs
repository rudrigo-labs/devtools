using System.IO;

namespace DevTools.Host.Wpf.Services;

public static class DotNetProjectValidator
{
    private static readonly string[] DotNetProjectExtensions = { "*.csproj", "*.sln", "*.slnx" };

    /// <summary>
    /// Verifica se existe pelo menos um arquivo .csproj, .sln ou .slnx
    /// diretamente na pasta raiz informada (não recursivo).
    /// </summary>
    public static bool HasDotNetProject(string? rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            return false;

        return DotNetProjectExtensions.Any(pattern =>
            Directory.EnumerateFiles(rootPath, pattern, SearchOption.TopDirectoryOnly).Any());
    }
}
