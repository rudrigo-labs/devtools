namespace DevTools.Presentation.Wpf.Persistence.Stores;

public interface INoteMetadataStore
{
    NoteMetadataRecord? GetByKey(string noteKey);
    void Upsert(NoteMetadataRecord record);
    void Delete(string noteKey);
}

