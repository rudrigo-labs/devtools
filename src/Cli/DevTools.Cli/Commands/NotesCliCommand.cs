using DevTools.Cli.Ui;
using DevTools.Cli.Logging;
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

    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        _ui.Section("Acoes");
        _ui.WriteLine("1) Listar notas");
        _ui.WriteLine("2) Ler nota");
        _ui.WriteLine("3) Criar nota");
        _ui.WriteLine("4) Exportar backup (ZIP)");
        _ui.WriteLine("5) Importar backup (ZIP)");

        var choice = _input.ReadInt("Escolha", 1, 5);
        var action = choice switch
        {
            1 => NotesAction.ListItems,
            2 => NotesAction.LoadNote,
            3 => NotesAction.CreateItem,
            4 => NotesAction.ExportZip,
            5 => NotesAction.ImportZip,
            _ => NotesAction.ListItems
        };

        string? noteKey = null;
        string? notesRoot = null;
        string? content = null;
        string? title = null;
        string? outputPath = null;
        string? zipPath = null;
        bool overwrite = true;

        notesRoot = _input.ReadOptional("Pasta das notas (opcional)", "enter = padrao");

        switch (action)
        {
            case NotesAction.ListItems:
                // No extra params needed
                break;

            case NotesAction.LoadNote:
                noteKey = _input.ReadRequired("Caminho/Nome da nota", "ex: 2023/10/minha-nota.md");
                break;

            case NotesAction.CreateItem:
                title = _input.ReadRequired("Titulo da nota", "ex: Reuniao Daily");
                content = _input.ReadMultiline("Conteudo da nota", "linha a linha", ".");
                break;

            case NotesAction.ExportZip:
                outputPath = _input.ReadRequired("Caminho para salvar o ZIP", "ex: C:\\Backup\\notes.zip");
                break;

            case NotesAction.ImportZip:
                zipPath = _input.ReadRequired("Caminho do ZIP origem", "ex: C:\\Downloads\\notes.zip");
                overwrite = _input.ReadYesNo("Sobrescrever existentes", false);
                break;
        }

        var request = new NotesRequest(
            Action: action,
            NoteKey: noteKey,
            Content: content,
            NotesRootPath: string.IsNullOrWhiteSpace(notesRoot) ? null : notesRoot,
            ConfigPath: null,
            Overwrite: overwrite,
            Title: title,
            OutputPath: outputPath,
            ZipPath: zipPath,
            CreateDateFolder: true, // Defaulting to organized folders
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
                _ui.WriteLine($"Backup exportado para: {outputPath ?? "destino"}");
            else if (action == NotesAction.ImportZip) 
                _ui.WriteLine("Importacao concluida com sucesso.");
            else 
                _ui.WriteLine("Operacao realizada com sucesso.");
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
