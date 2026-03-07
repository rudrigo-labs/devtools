using System;
using System.Linq;
using DevTools.Infrastructure.Persistence.Entities;
using DevTools.Infrastructure.Diagnostics;
using DevTools.Notes.Entities;
using DevTools.Notes.Repositories;
using Microsoft.EntityFrameworkCore;

namespace DevTools.Infrastructure.Persistence.Stores;

public sealed class SqliteNoteMetadataStore : INoteMetadataRepository
{
    private readonly DbContextOptions<DevToolsDbContext> _dbOptions;

    public SqliteNoteMetadataStore(DbContextOptions<DevToolsDbContext> dbOptions)
    {
        _dbOptions = dbOptions;
    }

    public NoteMetadataEntity? GetByKey(string noteKey)
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
            InfraLogger.Error($"Failed to read note metadata '{noteKey}' from SQLite.", ex);
            return null;
        }
    }

    public void Upsert(NoteMetadataEntity record)
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
            InfraLogger.Error($"Failed to upsert note metadata '{record.NoteKey}' in SQLite.", ex);
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
            InfraLogger.Error($"Failed to delete note metadata '{noteKey}' from SQLite.", ex);
        }
    }

    private static NoteMetadataEntity ToRecord(NoteIndexEntity row)
    {
        return new NoteMetadataEntity
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


