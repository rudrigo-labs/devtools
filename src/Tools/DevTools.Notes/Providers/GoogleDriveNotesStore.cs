using DevTools.Core.Results;
using DevTools.Notes.Abstractions;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using System.Text;

namespace DevTools.Notes.Providers;

public sealed class GoogleDriveNotesStore : IGoogleDriveNotesStore
{
    private readonly DriveService _service;
    private readonly string _folderId;

    public GoogleDriveNotesStore(DriveService service, string folderId)
    {
        _service = service;
        _folderId = folderId;
    }

    public async Task<RunResult<string>> UploadAsync(
        string fileName,
        string content,
        string mimeType,
        CancellationToken ct = default)
    {
        try
        {
            var existingId = await FindFileIdAsync(fileName, ct);
            var bytes = Encoding.UTF8.GetBytes(content);
            using var stream = new MemoryStream(bytes);

            if (existingId is null)
            {
                // Criar novo arquivo
                var meta = new Google.Apis.Drive.v3.Data.File
                {
                    Name = fileName,
                    Parents = new[] { _folderId },
                    MimeType = mimeType
                };
                var req = _service.Files.Create(meta, stream, mimeType);
                req.Fields = "id";
                var result = await req.UploadAsync(ct);
                if (result.Status == Google.Apis.Upload.UploadStatus.Failed)
                    return RunResult<string>.Fail(new ErrorDetail(
                        "notes.drive.upload.failed",
                        $"Falha ao criar arquivo no Drive: {result.Exception?.Message}",
                        Exception: result.Exception));

                return RunResult<string>.Success(req.ResponseBody?.Id ?? string.Empty);
            }
            else
            {
                // Atualizar existente
                var meta = new Google.Apis.Drive.v3.Data.File { Name = fileName };
                var req = _service.Files.Update(meta, existingId, stream, mimeType);
                req.Fields = "id";
                var result = await req.UploadAsync(ct);
                if (result.Status == Google.Apis.Upload.UploadStatus.Failed)
                    return RunResult<string>.Fail(new ErrorDetail(
                        "notes.drive.update.failed",
                        $"Falha ao atualizar arquivo no Drive: {result.Exception?.Message}",
                        Exception: result.Exception));

                return RunResult<string>.Success(existingId);
            }
        }
        catch (Exception ex)
        {
            return RunResult<string>.Fail(new ErrorDetail(
                "notes.drive.upload.error",
                "Erro inesperado ao enviar para o Google Drive.",
                Exception: ex));
        }
    }

    public async Task<RunResult<string?>> DownloadAsync(string fileName, CancellationToken ct = default)
    {
        try
        {
            var fileId = await FindFileIdAsync(fileName, ct);
            if (fileId is null)
                return RunResult<string?>.Success(null);

            var req = _service.Files.Get(fileId);
            using var ms = new MemoryStream();
            await req.DownloadAsync(ms, ct);
            return RunResult<string?>.Success(Encoding.UTF8.GetString(ms.ToArray()));
        }
        catch (Exception ex)
        {
            return RunResult<string?>.Fail(new ErrorDetail(
                "notes.drive.download.error",
                "Erro ao baixar arquivo do Google Drive.",
                Exception: ex));
        }
    }

    public async Task<RunResult<IReadOnlyList<string>>> ListFileNamesAsync(CancellationToken ct = default)
    {
        try
        {
            var req = _service.Files.List();
            req.Q = $"'{_folderId}' in parents and trashed = false";
            req.Fields = "files(name)";
            req.PageSize = 1000;
            var result = await req.ExecuteAsync(ct);
            var names = result.Files.Select(f => f.Name).ToList();
            return RunResult<IReadOnlyList<string>>.Success(names);
        }
        catch (Exception ex)
        {
            return RunResult<IReadOnlyList<string>>.Fail(new ErrorDetail(
                "notes.drive.list.error",
                "Erro ao listar arquivos no Google Drive.",
                Exception: ex));
        }
    }

    public async Task<RunResult<bool>> DeleteAsync(string fileName, CancellationToken ct = default)
    {
        try
        {
            var fileId = await FindFileIdAsync(fileName, ct);
            if (fileId is null)
                return RunResult<bool>.Success(false);

            await _service.Files.Delete(fileId).ExecuteAsync(ct);
            return RunResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            return RunResult<bool>.Fail(new ErrorDetail(
                "notes.drive.delete.error",
                "Erro ao deletar arquivo no Google Drive.",
                Exception: ex));
        }
    }

    private async Task<string?> FindFileIdAsync(string fileName, CancellationToken ct)
    {
        var req = _service.Files.List();
        req.Q = $"'{_folderId}' in parents and name = '{EscapeQuery(fileName)}' and trashed = false";
        req.Fields = "files(id)";
        req.PageSize = 1;
        var result = await req.ExecuteAsync(ct);
        return result.Files.FirstOrDefault()?.Id;
    }

    private static string EscapeQuery(string value) =>
        value.Replace("\\", "\\\\").Replace("'", "\\'");
}
