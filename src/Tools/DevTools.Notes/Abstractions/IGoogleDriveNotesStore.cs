using DevTools.Core.Results;
using DevTools.Notes.Models;

namespace DevTools.Notes.Abstractions;

public interface IGoogleDriveNotesStore
{
    /// <summary>Faz upload (create ou update) de um arquivo no Drive.</summary>
    Task<RunResult<string>> UploadAsync(string fileName, string content, string mimeType, CancellationToken ct = default);

    /// <summary>Baixa o conteúdo de um arquivo pelo nome.</summary>
    Task<RunResult<string?>> DownloadAsync(string fileName, CancellationToken ct = default);

    /// <summary>Lista os arquivos na pasta configurada.</summary>
    Task<RunResult<IReadOnlyList<string>>> ListFileNamesAsync(CancellationToken ct = default);

    /// <summary>Deleta um arquivo pelo nome.</summary>
    Task<RunResult<bool>> DeleteAsync(string fileName, CancellationToken ct = default);
}
