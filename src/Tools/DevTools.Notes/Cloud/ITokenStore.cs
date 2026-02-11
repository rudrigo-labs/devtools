namespace DevTools.Notes.Cloud;

public interface ITokenStore
{
    void SaveToken(string key, string token);
    string? GetToken(string key);
    void DeleteToken(string key);
}
