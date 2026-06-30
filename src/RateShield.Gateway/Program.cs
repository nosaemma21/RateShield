using RateShield.Gateway.Extensions;

var builder = WebApplication.CreateBuilder(args);

//addeing the rateshield options
builder
    .Services.AddRateShieldOptions(builder.Configuration)
    .AddRateShieldReverseProxy(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => "RateShield gateway is running.");

// using the rate shield rv proxy
app.MapReverseProxy();

app.Run();
