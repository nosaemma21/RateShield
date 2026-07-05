namespace RateShield.Core.Configuration;

public sealed class RedisOptions
{
    ///<summary>
    /// Redis cache connection string
    /// </summary>
    public string ConnectionString { get; set; } = String.Empty;
}
