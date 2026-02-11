namespace DevTools.Cli.Commands;

public interface ICliCommand
{
    string Key { get; }
    string Name { get; }
    string Description { get; }
    Task<int> ExecuteAsync(CancellationToken ct);
}
