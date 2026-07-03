namespace RateShield.Core.Configuration;

public sealed class ObservabilityOptions
{
    public bool Enabled { get; set; } = true;
    public string MetricsExporter { get; init; } = "Console";
}
