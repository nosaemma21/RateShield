using RateShield.Core.Time;

namespace RateShield.Core.Tests.Time;

public sealed class FakeClock : IClock
{
    public FakeClock(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; private set; }

    public void Advance(TimeSpan time)
    {
        UtcNow = UtcNow.Add(time);
    }
}
