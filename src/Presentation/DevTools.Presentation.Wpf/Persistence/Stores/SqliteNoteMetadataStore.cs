using System;
using System.Linq;
using DevTools.Presentation.Wpf.Persistence.Entities;
using DevTools.Presentation.Wpf.Services;
using Microsoft.EntityFrameworkCore;

namespace DevTools.Presentation.Wpf.Persistence.Stores;

public sealed class SqliteNoteMetadataStore : INoteMetadataStore
{
    private readonly DbContextOptions<DevToolsDbContext> _dbOptions;

    public SqliteNoteMetadataStore(DbContextOptions<DevToolsDbContext> dbOptions)
    {
        _dbOptions = dbOptions;
    }

    public NoteMetadataRecord? GetByKey(string noteKey)
    {
        if (string.IsNullOrWhiteSpace(noteKey))
        {
            return null;
        }

        try
        {
            using var db = new DevToolsDbContext(_dbOptions);
            var row = db.NoteIndex.SingleOrDefault(x => x.NoteKey == noteKey);
            return row == null ? null : ToRecord(row);
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to read note metadata '{noteKey}' from SQLite.", ex);
            return null;
        }
    }

    public void Upsert(NoteMetadataRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.NoteKey))
        {
            return;
        }

        try
        {
            using var db = new DevToolsDbContext(_dbOptions);
            var row = db.NoteIndex.SingleOrDefault(x => x.NoteKey == record.NoteKey);
            if (row == null)
            {
                row = new NoteIndexEntity
                {
                    NoteKey = record.NoteKey
                };
                db.NoteIndex.Add(row);
            }

            row.Title = record.Title;
            row.Extension = record.Extension;
            row.LastLocalWriteUtc = record.LastLocalWriteUtc;
            row.LastCloudSyncUtc = record.LastCloudSyncUtc;
            row.LastCloudStatus = record.LastCloudStatus;
            row.Hash = record.Hash;

            db.SaveChanges();
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to upsert note metadata '{record.NoteKey}' in SQLite.", ex);
        }
    }

    public void Delete(string noteKey)
    {
        if (string.IsNullOrWhiteSpace(noteKey))
        {
            return;
        }

        try
        {
            using var db = new DevToolsDbContext(_dbOptions);
            var row = db.NoteIndex.SingleOrDefault(x => x.NoteKey == noteKey);
            if (row != null)
            {
                db.NoteIndex.Remove(row);
                db.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to delete note metadata '{noteKey}' from SQLite.", ex);
        }
    }

    private static NoteMetadataRecord ToRecord(NoteIndexEntity row)
    {
        return new NoteMetadataRecord
        {
            NoteKey = row.NoteKey,
            Title = row.Title,
            Extension = row.Extension,
            LastLocalWriteUtc = row.LastLocalWriteUtc,
            LastCloudSyncUtc = row.LastCloudSyncUtc,
            LastCloudStatus = row.LastCloudStatus,
            Hash = row.Hash
        };
    }
}

