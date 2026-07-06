namespace RateShield.Core.Configuration;

public sealed class RedisOptions
{
    ///<summary>
    /// Redis cache connection string
    /// </summary>
    public string ConnectionString { get; set; } = String.Empty;

    /// <summary>
    /// How long rateshield will wait while connection go redis first
    /// </summary>
    public int ConnectTimeoutMilliseconds { get; init; } = 5000;

    /// <summary>
    /// time out for operations like my lua
    /// </summary>
    public int CommandTimeoutMilliseconds { get; init; } = 1000;
}
