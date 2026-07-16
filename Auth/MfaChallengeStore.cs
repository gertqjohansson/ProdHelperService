using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace ProdHelperService.Auth;

public class MfaChallengeStore(IMemoryCache cache) : IMfaChallengeStore
{
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);

    public string CreateChallenge(string userId)
    {
        string token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        cache.Set(CacheKey(token), userId, ChallengeLifetime);
        return token;
    }

    public string? Resolve(string challengeToken) =>
        cache.TryGetValue(CacheKey(challengeToken), out string? userId) ? userId : null;

    public void Consume(string challengeToken) => cache.Remove(CacheKey(challengeToken));

    private static string CacheKey(string token) => $"mfa-challenge:{token}";
}
