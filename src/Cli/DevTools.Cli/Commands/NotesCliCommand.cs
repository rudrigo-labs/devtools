using DevTools.Cli.Ui;
using DevTools.Cli.Logging;
using DevTools.Cli.App;
using DevTools.Notes.Engine;
using DevTools.Notes.Models;

namespace DevTools.Cli.Commands;

public sealed class NotesCliCommand : ICliCommand
{
    private readonly CliConsole _ui;
    private readonly CliInput _input;
    private readonly NotesEngine _engine;

    public NotesCliCommand(CliConsole ui, CliInput input)
    {
        _ui = ui;
        _input = input;
        _engine = new NotesEngine();
    }

    public string Key => "notes";
    public string Name => "Notes";
    public string Description => "Le e salva notas (local).";

    public async Task<int> ExecuteAsync(CliLaunchOptions options, CancellationToken ct)
    {
        // 1. Resolve Action
        var actionStr = options.GetOption("action");
        NotesAction? action = null;
        if (actionStr != null)
        {
            if (Enum.TryParse<NotesAction>(actionStr, true, out var a)) action = a;
            else if (actionStr.Equals("list", StringComparison.OrdinalIgnoreCase)) action = NotesAction.ListItems;
            else if (actionStr.Equals("read", StringComparison.OrdinalIgnoreCase)) action = NotesAction.LoadNote;
            else if (actionStr.Equals("create", StringComparison.OrdinalIgnoreCase)) action = NotesAction.CreateItem;
            else if (actionStr.Equals("export", StringComparison.OrdinalIgnoreCase)) action = NotesAction.ExportZip;
            else if (actionStr.Equals("import", StringComparison.OrdinalIgnoreCase)) action = NotesAction.ImportZip;
        }

        if (action == null && !options.IsNonInteractive)
        {
            _ui.Section("Acoes");
            _ui.WriteLine("1) Listar notas");
            _ui.WriteLine("2) Ler nota");
            _ui.WriteLine("3) Criar nota");
            _ui.WriteLine("4) Exportar backup (ZIP)");
            _ui.WriteLine("5) Importar backup (ZIP)");

            var choice = _input.ReadInt("Escolha", 1, 5);
            action = choice switch
            {
                1 => NotesAction.ListItems,
                2 => NotesAction.LoadNote,
                3 => NotesAction.CreateItem,
                4 => NotesAction.ExportZip,
                5 => NotesAction.ImportZip,
                _ => NotesAction.ListItems
            };
        }

        if (action == null)
        {
            _ui.WriteError("Action required (--action list|read|create|export|import).");
            return 1;
        }

        // 2. Resolve Parameters
        var root = options.GetOption("root") ?? options.GetOption("path");
        var key = options.GetOption("key") ?? options.GetOption("note");
        var title = options.GetOption("title");
        var content = options.GetOption("content");
        var output = options.GetOption("output") ?? options.GetOption("out");
        var zip = options.GetOption("zip") ?? options.GetOption("source");
        var overwriteStr = options.GetOption("overwrite");
        bool? overwrite = overwriteStr != null ? (overwriteStr == "true") : null;

        // Interactive Fallback
        if (!options.IsNonInteractive)
        {
            if (string.IsNullOrWhiteSpace(root))
                root = _input.ReadOptional("Pasta das notas (opcional)", "enter = padrao");

            switch (action)
            {
                case NotesAction.LoadNote:
                    if (string.IsNullOrWhiteSpace(key))
                        key = _input.ReadRequired("Caminho/Nome da nota", "ex: 2023/10/minha-nota.md");
                    break;
                case NotesAction.CreateItem:
                    if (string.IsNullOrWhiteSpace(title))
                        title = _input.ReadRequired("Titulo da nota", "ex: Reuniao Daily");
                    if (string.IsNullOrWhiteSpace(content))
                        content = _input.ReadMultiline("Conteudo da nota", "linha a linha", ".");
                    break;
                case NotesAction.ExportZip:
                    if (string.IsNullOrWhiteSpace(output))
                        output = _input.ReadRequired("Caminho para salvar o ZIP", "ex: C:\\Backup\\notes.zip");
                    break;
                case NotesAction.ImportZip:
                    if (string.IsNullOrWhiteSpace(zip))
                        zip = _input.ReadRequired("Caminho do ZIP origem", "ex: C:\\Downloads\\notes.zip");
                    if (overwrite == null)
                        overwrite = _input.ReadYesNo("Sobrescrever existentes", false);
                    break;
            }
        }

        // Defaults
        overwrite ??= false;

        // Validation
        if (action == NotesAction.LoadNote && string.IsNullOrWhiteSpace(key))
        {
            _ui.WriteError("Key/Path required for read action (--key).");
            return 1;
        }
        if (action == NotesAction.CreateItem && string.IsNullOrWhiteSpace(title))
        {
            _ui.WriteError("Title required for create action (--title).");
            return 1;
        }
        if (action == NotesAction.ExportZip && string.IsNullOrWhiteSpace(output))
        {
            _ui.WriteError("Output path required for export action (--output).");
            return 1;
        }
        if (action == NotesAction.ImportZip && string.IsNullOrWhiteSpace(zip))
        {
            _ui.WriteError("Zip path required for import action (--zip).");
            return 1;
        }

        var request = new NotesRequest(
            Action: action.Value,
            NoteKey: key,
            Content: content,
            NotesRootPath: string.IsNullOrWhiteSpace(root) ? null : root,
            ConfigPath: null,
            Overwrite: overwrite.Value,
            Title: title,
            OutputPath: output,
            ZipPath: zip,
            CreateDateFolder: true,
            UseMarkdown: true
        );

        using var progress = new CliProgressReporter(_ui.Theme);
        var result = await _engine.ExecuteAsync(request, progress, ct).ConfigureAwait(false);
        progress.Finish();

        if (!result.IsSuccess || result.Value is null)
        {
            WriteErrors(result.Errors);
            return 1;
        }

        var response = result.Value;

        if (!options.IsNonInteractive)
        {
            _ui.Section("Resultado");

            if (action == NotesAction.ListItems && response.ListResult != null)
            {
                _ui.WriteLine($"Total de notas: {response.ListResult.Items.Count}");
                foreach (var item in response.ListResult.Items)
                {
                    _ui.WriteLine($"- {item.FileName} ({item.UpdatedUtc})");
                }
            }
            else if (action == NotesAction.LoadNote && response.ReadResult != null)
            {
                _ui.Section("Nota Carregada");
                _ui.WriteLine(response.ReadResult.Content ?? "(Vazio)");
            }
            else
            {
                if (action == NotesAction.CreateItem)
                    _ui.WriteLine("Nota criada com sucesso!");
                else if (action == NotesAction.ExportZip)
                    _ui.WriteLine($"Backup exportado para: {output ?? "destino"}");
                else if (action == NotesAction.ImportZip)
                    _ui.WriteLine("Importacao concluida com sucesso.");
                else
                    _ui.WriteLine("Operacao realizada com sucesso.");
            }
        }
        else
        {
            // Non-interactive output
            if (action == NotesAction.ListItems && response.ListResult != null)
            {
                foreach (var item in response.ListResult.Items)
                {
                    _ui.WriteLine($"{item.FileName}\t{item.UpdatedUtc}");
                }
            }
            else if (action == NotesAction.LoadNote && response.ReadResult != null)
            {
                _ui.WriteLine(response.ReadResult.Content ?? "");
            }
            else if (action == NotesAction.ExportZip)
            {
                _ui.WriteLine(output ?? "Exported");
            }
        }

        return 0;
    }

    private void WriteErrors(IReadOnlyList<DevTools.Core.Results.ErrorDetail> errors)
    {
        CliErrorLogger.LogErrors(Key, errors);
        _ui.Section("Erros");
        foreach (var error in errors)
        {
            _ui.WriteError($"{error.Code}: {error.Message}");
            if (!string.IsNullOrWhiteSpace(error.Details))
                _ui.WriteDim(error.Details);
        }
    }
}
