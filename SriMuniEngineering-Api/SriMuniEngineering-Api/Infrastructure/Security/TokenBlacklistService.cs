using System.Collections.Concurrent;

namespace SriMuniEngineering_Api.Infrastructure.Security;

public class TokenBlacklistService
{
    private readonly ConcurrentDictionary<string, DateTime> _blacklistedTokens = new();

    public void BlacklistToken(string jti, DateTime expiry)
    {
        _blacklistedTokens.TryAdd(jti, expiry);
        CleanupExpiredEntries();
    }

    public bool IsBlacklisted(string jti)
    {
        return _blacklistedTokens.ContainsKey(jti);
    }

    private void CleanupExpiredEntries()
    {
        var now = DateTime.Now;
        var expiredKeys = _blacklistedTokens
            .Where(kvp => kvp.Value < now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _blacklistedTokens.TryRemove(key, out _);
        }
    }
}
