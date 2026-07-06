using System.Security.Cryptography;
using System.Text;
using RateShield.Core.Identity;
using RateShield.Core.RateLimiting;

namespace RateShield.Infrastructure.RateLimiting.Redis;

public static class RedisTokenBucketKeyBuilder
{
    /// <summary>
    /// Builds redis key for client
    /// </summary>
    /// <param name="client">client name</param>
    /// <param name="routeId">current route id</param>
    /// <param name="policy">the rate limit policy</param>
    /// <returns></returns>
    public static string Build(ClientIdentity client, string routeId, RateLimitPolicy policy)
    {
        var clientHash = HashClientIdentity(client);

        return $"rateshield:v1:bucket:{routeId}:{policy.Name}:{clientHash}";
    }

    /// <summary>
    /// helper to hash client id
    /// </summary>
    /// <param name="client"></param>
    /// <returns>hashed client id</returns>
    private static string HashClientIdentity(ClientIdentity client)
    {
        var rawIdentity = $"{client.Source}:{client.Value}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawIdentity));

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
