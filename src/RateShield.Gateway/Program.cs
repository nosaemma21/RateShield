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

app.UseRouting(); //--------------\\
app.UseRateShieldRateLimiting(); //------------route before mw so "context.GetEndpoint()" is not null

app.MapRateShieldEndpoints();

app.Run();
