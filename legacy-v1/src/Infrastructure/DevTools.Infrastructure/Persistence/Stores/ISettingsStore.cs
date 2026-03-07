namespace DevTools.Infrastructure.Persistence.Stores;

public interface ISettingsStore
{
    string Location { get; }
    bool IsConfigured();
    T GetSection<T>(string sectionName) where T : new();
    void SaveSection<T>(string sectionName, T data);
    void CreateDefaultIfNotExists();
}


