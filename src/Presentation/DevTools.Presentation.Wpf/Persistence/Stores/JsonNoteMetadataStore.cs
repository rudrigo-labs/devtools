using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DevTools.Presentation.Wpf.Services;

namespace DevTools.Presentation.Wpf.Persistence.Stores;

public sealed class JsonNoteMetadataStore : INoteMetadataStore
{
    private readonly string _filePath;

    public JsonNoteMetadataStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var root = Path.Combine(appData, "DevTools");
        Directory.CreateDirectory(root);
        _filePath = Path.Combine(root, "note_index.json");
    }

    public NoteMetadataRecord? GetByKey(string noteKey)
    {
        if (string.IsNullOrWhiteSpace(noteKey))
        {
            return null;
        }

        var dict = ReadAll();
        return dict.TryGetValue(noteKey, out var record) ? record : null;
    }

    public void Upsert(NoteMetadataRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.NoteKey))
        {
            return;
        }

        var dict = ReadAll();
        dict[record.NoteKey] = record;
        WriteAll(dict);
    }

    public void Delete(string noteKey)
    {
        if (string.IsNullOrWhiteSpace(noteKey))
        {
            return;
        }

        var dict = ReadAll();
        if (dict.Remove(noteKey))
        {
            WriteAll(dict);
        }
    }

    private Dictionary<string, NoteMetadataRecord> ReadAll()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new Dictionary<string, NoteMetadataRecord>(StringComparer.OrdinalIgnoreCase);
            }

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<Dictionary<string, NoteMetadataRecord>>(json)
                   ?? new Dictionary<string, NoteMetadataRecord>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            AppLogger.Error("Failed to read JSON note metadata store.", ex);
            return new Dictionary<string, NoteMetadataRecord>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void WriteAll(Dictionary<string, NoteMetadataRecord> records)
    {
        try
        {
            var json = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            AppLogger.Error("Failed to write JSON note metadata store.", ex);
        }
    }
}

