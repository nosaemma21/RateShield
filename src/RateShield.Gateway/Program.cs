using RateShield.Gateway.Extensions;

var builder = WebApplication.CreateBuilder(args);

//graceful shutdown for bg service
builder.Host.ConfigureHostOptions(opitons =>
{
    opitons.ShutdownTimeout = TimeSpan.FromSeconds(30);
});

//adding the rateshield options
builder
    .Services.AddRateShieldOptions(builder.Configuration)
    .AddRateShieldApplicationServices(builder.Configuration)
    .AddRateShieldReverseProxy(builder.Configuration)
    .AddRateShieldHealthChecks(builder.Configuration)
    .AddRateShieldObservability(builder.Configuration);

var app = builder.Build();

// app.MapGet("/", () => "RateShield gateway is running.");

// using the rate shield rv proxy
// app.MapReverseProxy();

// endpoints in their file

app.UseRateShieldExceptionHandling();

app.UseRouting(); //so "context.GetEndpoint()" is not null
app.UseRequestTimeouts();
app.UseRateShieldCorrelationId();
app.UseRateShieldRateLimiting();

app.MapRateShieldEndpoints();

app.Run();

//for test visibility
public partial class Program { }
