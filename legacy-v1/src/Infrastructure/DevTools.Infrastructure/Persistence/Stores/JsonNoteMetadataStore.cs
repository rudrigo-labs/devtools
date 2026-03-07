using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DevTools.Infrastructure.Diagnostics;
using DevTools.Notes.Entities;
using DevTools.Notes.Repositories;

namespace DevTools.Infrastructure.Persistence.Stores;

public sealed class JsonNoteMetadataStore : INoteMetadataRepository
{
    private readonly string _filePath;

    public JsonNoteMetadataStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var root = Path.Combine(appData, "DevTools");
        Directory.CreateDirectory(root);
        _filePath = Path.Combine(root, "note_index.json");
    }

    public NoteMetadataEntity? GetByKey(string noteKey)
    {
        if (string.IsNullOrWhiteSpace(noteKey))
        {
            return null;
        }

        var dict = ReadAll();
        return dict.TryGetValue(noteKey, out var record) ? record : null;
    }

    public void Upsert(NoteMetadataEntity record)
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

    private Dictionary<string, NoteMetadataEntity> ReadAll()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new Dictionary<string, NoteMetadataEntity>(StringComparer.OrdinalIgnoreCase);
            }

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<Dictionary<string, NoteMetadataEntity>>(json)
                   ?? new Dictionary<string, NoteMetadataEntity>(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            InfraLogger.Error("Failed to read JSON note metadata store.", ex);
            return new Dictionary<string, NoteMetadataEntity>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private void WriteAll(Dictionary<string, NoteMetadataEntity> records)
    {
        try
        {
            var json = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            InfraLogger.Error("Failed to write JSON note metadata store.", ex);
        }
    }
}


