using RateShield.Gateway.Extensions;

var builder = WebApplication.CreateBuilder(args);

//addeing the rateshield options
builder.Services.AddRateShieldOptions(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => "RateShield gateway is running.");

app.Run();
