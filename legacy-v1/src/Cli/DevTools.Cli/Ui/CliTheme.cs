namespace DevTools.Cli.Ui;

public sealed class CliTheme
{
    public ConsoleColor TitleColor { get; init; } = ConsoleColor.Cyan;
    public ConsoleColor AccentColor { get; init; } = ConsoleColor.DarkCyan;
    public ConsoleColor InfoColor { get; init; } = ConsoleColor.Gray;
    public ConsoleColor SuccessColor { get; init; } = ConsoleColor.Green;
    public ConsoleColor WarningColor { get; init; } = ConsoleColor.Yellow;
    public ConsoleColor ErrorColor { get; init; } = ConsoleColor.Red;
    public ConsoleColor DimColor { get; init; } = ConsoleColor.DarkGray;
    public ConsoleColor InputLabelColor { get; init; } = ConsoleColor.White;
    public ConsoleColor InputBoxColor { get; init; } = ConsoleColor.Cyan;
}
