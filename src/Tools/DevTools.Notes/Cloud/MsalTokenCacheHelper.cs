using System.IO;
using Microsoft.Identity.Client;

namespace DevTools.Notes.Cloud;

public class MsalTokenCacheHelper
{
    private readonly ITokenStore _tokenStore;
    private readonly string _cacheKey;

    public MsalTokenCacheHelper(ITokenStore tokenStore, string cacheKey = "msal_cache")
    {
        _tokenStore = tokenStore;
        _cacheKey = cacheKey;
    }

    public void EnableSerialization(ITokenCache tokenCache)
    {
        tokenCache.SetBeforeAccess(BeforeAccessNotification);
        tokenCache.SetAfterAccess(AfterAccessNotification);
    }

    private void BeforeAccessNotification(TokenCacheNotificationArgs args)
    {
        var serializedData = _tokenStore.GetToken(_cacheKey);
        if (!string.IsNullOrEmpty(serializedData))
        {
            // The data in TokenStore is Base64 string of the bytes (implied by encoding usually)
            // But wait, my TokenStore stores string. MSAL cache is byte[].
            // I should convert.
            // My SecureTokenStore treats input as string, encrypts bytes, saves bytes.
            // But GetToken returns string (UTF8).
            // MSAL cache blob is binary. Converting arbitrary binary to UTF8 string is risky.
            // I should probably Base64 encode the MSAL cache before sending to TokenStore.
            
            try 
            {
                var bytes = System.Convert.FromBase64String(serializedData);
                args.TokenCache.DeserializeMsalV3(bytes);
            }
            catch
            {
                // If fails, clear cache
                args.TokenCache.DeserializeMsalV3(null);
            }
        }
    }

    private void AfterAccessNotification(TokenCacheNotificationArgs args)
    {
        // if the access operation resulted in a cache update
        if (args.HasStateChanged)
        {
            var bytes = args.TokenCache.SerializeMsalV3();
            var base64 = System.Convert.ToBase64String(bytes);
            _tokenStore.SaveToken(_cacheKey, base64);
        }
    }
}
