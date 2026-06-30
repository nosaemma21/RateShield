var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet(
    "/",
    () => new { Service = "RateShieled.SampleBackend", Message = "Sample backend running..." }
);

app.MapGet(
    "/api/{**catchAll}",
    (HttpContext context, string? catchAll) =>
        new
        {
            Service = "RateShield.SampleBackend",
            Method = context.Request.Method,
            Path = context.Request.Path.Value,
            QueryString = context.Request.QueryString.Value,
            CatchAll = catchAll,
        }
);

app.Run();
