using DevTools.Cli.App;
using DevTools.Cli.Commands;
using DevTools.Cli.Ui;

CliConsole.ConfigureEncoding();

var theme = new CliTheme();
var state = new CliState();
var ui = new CliConsole(theme, state);
var input = new CliInput(ui);
var menu = new CliMenu(ui, input);

var commands = new List<ICliCommand>
{
    new HarvestCliCommand(ui, input),
    new SnapshotCliCommand(ui, input),
    new SearchTextCliCommand(ui, input),
    new RenameCliCommand(ui, input),
    new Utf8ConvertCliCommand(ui, input),
    new OrganizerCliCommand(ui, input),
    new ImageSplitCliCommand(ui, input),
    new MigrationsCliCommand(ui, input),
    new NgrokCliCommand(ui, input),
    new SshTunnelCliCommand(ui, input),
    new NotesCliCommand(ui, input)
};

var orderedCommands = commands
    .OrderBy(c => c.Key, StringComparer.OrdinalIgnoreCase)
    .ToList();

state.MenuItems = orderedCommands
    .Select((cmd, index) => $"{index + 1} - {cmd.Key.ToLowerInvariant()}")
    .ToList();

var app = new CliApp(ui, menu, input, orderedCommands);
return await app.RunAsync(CancellationToken.None);
