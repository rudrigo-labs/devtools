using System;
using System.Threading.Tasks;
using Google.Apis.Util.Store;
using Newtonsoft.Json;

namespace DevTools.Notes.Cloud;

public class GoogleTokenStoreAdapter : IDataStore
{
    private readonly ITokenStore _tokenStore;

    public GoogleTokenStoreAdapter(ITokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    public Task ClearAsync()
    {
        // We can't easily iterate all keys in our simple ITokenStore interface 
        // without adding a ListKeys method.
        // For now, this might be a limitation, or we just ignore it as 
        // clearing all tokens is rare in this context.
        // Or we implement a specific "Clear" method in ITokenStore if needed.
        return Task.CompletedTask;
    }

    public Task DeleteAsync<T>(string key)
    {
        _tokenStore.DeleteToken(GenerateKey(key));
        return Task.CompletedTask;
    }

    public Task<T> GetAsync<T>(string key)
    {
        var json = _tokenStore.GetToken(GenerateKey(key));
        if (string.IsNullOrEmpty(json))
        {
            return Task.FromResult(default(T)!);
        }

        try
        {
            var value = JsonConvert.DeserializeObject<T>(json);
            return Task.FromResult(value!);
        }
        catch
        {
            return Task.FromResult(default(T)!);
        }
    }

    public Task StoreAsync<T>(string key, T value)
    {
        var json = JsonConvert.SerializeObject(value);
        _tokenStore.SaveToken(GenerateKey(key), json);
        return Task.CompletedTask;
    }

    private string GenerateKey(string key) => $"google_auth_{key}";
}
