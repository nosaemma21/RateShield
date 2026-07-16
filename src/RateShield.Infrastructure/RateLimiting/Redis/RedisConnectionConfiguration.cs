using StackExchange.Redis;

namespace RateShield.Infrastructure.RateLimiting.Redis;

public static class RedisConnectionConfiguration
{
    public static ConfigurationOptions Create(
        string connectionString,
        int connectTimeoutMilliseconds,
        int commandTimeoutMilliseconds
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var configuration = IsRedisUri(connectionString)
            ? CreateFromUri(new Uri(connectionString, UriKind.Absolute))
            : ConfigurationOptions.Parse(connectionString);

        configuration.ConnectTimeout = connectTimeoutMilliseconds;
        configuration.AsyncTimeout = commandTimeoutMilliseconds;
        configuration.SyncTimeout = commandTimeoutMilliseconds;
        configuration.AbortOnConnectFail = false;

        return configuration;
    }

    private static bool IsRedisUri(string connectionString)
    {
        return connectionString.StartsWith("redis://", StringComparison.OrdinalIgnoreCase)
            || connectionString.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase);
    }

    private static ConfigurationOptions CreateFromUri(Uri uri)
    {
        if (string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new FormatException("The Redis connection URI must include a host.");
        }

        var useTls = string.Equals(uri.Scheme, "rediss", StringComparison.OrdinalIgnoreCase);
        var port = uri.Port > 0 ? uri.Port : useTls ? 6380 : 6379;

        var configuration = new ConfigurationOptions
        {
            Ssl = useTls,
            SslHost = useTls ? uri.Host : null,
        };

        configuration.EndPoints.Add(uri.Host, port);

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var credentials = uri.UserInfo.Split(':', 2);

            if (!string.IsNullOrEmpty(credentials[0]))
            {
                configuration.User = Uri.UnescapeDataString(credentials[0]);
            }

            if (credentials.Length == 2)
            {
                configuration.Password = Uri.UnescapeDataString(credentials[1]);
            }
        }

        var databasePath = uri.AbsolutePath.Trim('/');

        if (!string.IsNullOrEmpty(databasePath))
        {
            if (!int.TryParse(databasePath, out var database))
            {
                throw new FormatException("The Redis connection URI database must be an integer.");
            }

            configuration.DefaultDatabase = database;
        }

        return configuration;
    }
}
