using DevTools.Cli.Ui;
using DevTools.Cli.Logging;
using DevTools.Notes.Engine;
using DevTools.Notes.Models;
using DevTools.Notes.Cloud;

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
        _ui.WriteLine("6) Conectar Google Drive");
        _ui.WriteLine("7) Conectar OneDrive");
        _ui.WriteLine("8) Sincronizar Agora");
        _ui.WriteLine("9) Status Cloud");
        _ui.WriteLine("10) Desconectar Cloud");

        var choice = _input.ReadInt("Escolha", 1, 10);
        var action = choice switch
        {
            1 => NotesAction.ListItems,
            2 => NotesAction.LoadNote,
            3 => NotesAction.CreateItem,
            4 => NotesAction.ExportZip,
            5 => NotesAction.ImportZip,
            6 => NotesAction.ConnectGoogle,
            7 => NotesAction.ConnectOneDrive,
            8 => NotesAction.SyncCloud,
            9 => NotesAction.GetCloudStatus,
            10 => NotesAction.DisconnectCloud,
            _ => NotesAction.ListItems
        };

        // Load CLI settings
        var settings = CliNotesSettings.Load();

        // Auto-connect for Sync/Status if needed
        if (action == NotesAction.SyncCloud || action == NotesAction.GetCloudStatus || action == NotesAction.DisconnectCloud)
        {
            if (!string.IsNullOrEmpty(settings.LastCloudProvider) && 
                Enum.TryParse<CloudProviderType>(settings.LastCloudProvider, out var lastProvider))
            {
                // Silent connect attempt with default secrets handled by Engine
                var connectReq = new NotesRequest(
                    Action: lastProvider == CloudProviderType.GoogleDrive ? NotesAction.ConnectGoogle : NotesAction.ConnectOneDrive,
                    NotesRootPath: null,
                    CloudProvider: lastProvider,
                    CloudConfig: null // Use defaults in Engine
                );

                // We ignore the result of auto-connect; if it fails, the subsequent action will report "Not connected"
                await _engine.ExecuteAsync(connectReq, ct: ct).ConfigureAwait(false);
            }
        }

        string? noteKey = null;
        string? notesRoot = null;
        string? content = null;
        string? title = null;
        string? outputPath = null;
        string? zipPath = null;
        bool overwrite = true;
        CloudProviderType cloudProvider = CloudProviderType.None;

        // Common input for most actions
        if (action != NotesAction.GetCloudStatus && action != NotesAction.DisconnectCloud && action != NotesAction.ConnectGoogle && action != NotesAction.ConnectOneDrive)
        {
            notesRoot = _input.ReadOptional("Pasta das notas (opcional)", "enter = padrao");
        }

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

            case NotesAction.ConnectGoogle:
                cloudProvider = CloudProviderType.GoogleDrive;
                break;

            case NotesAction.ConnectOneDrive:
                cloudProvider = CloudProviderType.OneDrive;
                break;
        }

        // Use settings for config
        var cloudConfig = new CloudConfiguration
        {
            // Use persisted overrides if available, otherwise Engine uses CloudSecrets
            GoogleClientId = settings.GoogleClientId,
            GoogleClientSecret = settings.GoogleClientSecret,
            OneDriveClientId = settings.OneDriveClientId
        };

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
            UseMarkdown: true,
            CloudProvider: cloudProvider,
            CloudConfig: cloudConfig
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
        
        // Save settings on successful connect or disconnect
        if (result.IsSuccess)
        {
            if (action == NotesAction.ConnectGoogle)
            {
                settings.LastCloudProvider = CloudProviderType.GoogleDrive.ToString();
                settings.Save();
            }
            else if (action == NotesAction.ConnectOneDrive)
            {
                settings.LastCloudProvider = CloudProviderType.OneDrive.ToString();
                settings.Save();
            }
            else if (action == NotesAction.DisconnectCloud)
            {
                settings.LastCloudProvider = null;
                settings.Save();
            }
        }

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
        else if (action == NotesAction.SyncCloud && response.SyncResult != null)
        {
             var sync = response.SyncResult;
             _ui.WriteLine($"Enviados: {sync.Uploaded}");
             _ui.WriteLine($"Baixados: {sync.Downloaded}");
             _ui.WriteLine($"Conflitos: {sync.Conflicts}");
             _ui.WriteLine($"Erros: {sync.Errors}");
             if (sync.Messages.Any())
             {
                 _ui.Section("Detalhes");
                 foreach(var msg in sync.Messages) _ui.WriteLine(msg);
             }
        }
        else if (action == NotesAction.GetCloudStatus)
        {
            _ui.WriteLine($"Status: {(response.IsConnected ? "Conectado" : "Desconectado")}");
            _ui.WriteLine($"Usuario: {response.CloudUser ?? "N/A"}");
        }
        else
        {
            if (action == NotesAction.CreateItem) 
                _ui.WriteLine("Nota criada com sucesso!");
            else if (action == NotesAction.ExportZip) 
                _ui.WriteLine($"Backup exportado para: {outputPath ?? "destino"}");
            else if (action == NotesAction.ImportZip) 
                _ui.WriteLine("Importacao concluida com sucesso.");
            else if (action == NotesAction.ConnectGoogle || action == NotesAction.ConnectOneDrive)
                _ui.WriteLine("Conectado com sucesso!");
            else if (action == NotesAction.DisconnectCloud)
                _ui.WriteLine("Desconectado com sucesso!");
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

    private class CliNotesSettings
    {
        public string? GoogleClientId { get; set; }
        public string? GoogleClientSecret { get; set; }
        public string? OneDriveClientId { get; set; }
        public string? LastCloudProvider { get; set; }

        private static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DevTools", "cli_notes_settings.json");

        public static CliNotesSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    return System.Text.Json.JsonSerializer.Deserialize<CliNotesSettings>(json) ?? new CliNotesSettings();
                }
            }
            catch { }
            return new CliNotesSettings();
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(FilePath);
                if (!Directory.Exists(dir) && dir != null) Directory.CreateDirectory(dir);
                
                var json = System.Text.Json.JsonSerializer.Serialize(this);
                File.WriteAllText(FilePath, json);
            }
            catch { }
        }
    }
}
