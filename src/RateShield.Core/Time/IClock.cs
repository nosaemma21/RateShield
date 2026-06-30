namespace RateShield.Core.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
