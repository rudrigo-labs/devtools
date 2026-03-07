using DevTools.Notes.Entities;

namespace DevTools.Notes.Repositories;

public interface INoteMetadataRepository
{
    NoteMetadataEntity? GetByKey(string noteKey);
    void Upsert(NoteMetadataEntity entity);
    void Delete(string noteKey);
}
