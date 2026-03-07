namespace DevTools.Cli.Ui;

public sealed class CliAbortException : Exception
{
    public CliAbortException() : base("Operation cancelled by user.") { }
}
