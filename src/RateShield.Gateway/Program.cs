using RateShield.Gateway.Extensions;

var builder = WebApplication.CreateBuilder(args);

//adding the rateshield options
builder
    .Services.AddRateShieldOptions(builder.Configuration)
    .AddRateShieldApplicationServices()
    .AddRateShieldReverseProxy(builder.Configuration)
    .AddRateShieldHealthChecks();

var app = builder.Build();

// app.MapGet("/", () => "RateShield gateway is running.");

// using the rate shield rv proxy
// app.MapReverseProxy();

// endpoints in their file

app.UseRouting(); //so "context.GetEndpoint()" is not null
app.UseRateShieldCorrelationId();
app.UseRateShieldRateLimiting();

app.MapRateShieldEndpoints();

app.Run();

//for test visibility
public partial class Program { }
