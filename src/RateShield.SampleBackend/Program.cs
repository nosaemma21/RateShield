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

////////🧪🧪🧪🧪
app.MapGet(
    "/slow",
    async (HttpContext context) =>
    {
        await Task.Delay(TimeSpan.FromSeconds(3));

        return Results.Ok(
            new
            {
                Service = "RateShield.SampleBackend",
                Message = "Slow response completed",
                Method = context.Request.Method,
                Path = context.Request.Path.Value,
            }
        );
    }
);

app.MapGet(
    "/fail",
    () =>
    {
        return Results.Problem(
            title: "Simulated backend failure",
            detail: "This endpoint intentionally returns a backend error for gateway testing.",
            statusCode: StatusCodes.Status500InternalServerError
        );
    }
);

app.MapPost(
    "/echo",
    async (HttpContext context) =>
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();

        return Results.Ok(
            new
            {
                Service = "RateShield.SampleBackend",
                Method = context.Request.Method,
                Path = context.Request.Path.Value,
                ContentType = context.Request.ContentType,
                Body = body,
            }
        );
    }
);

app.MapGet(
    "/headers",
    (HttpContext context) =>
    {
        return Results.Ok(
            new
            {
                Service = "RateShield.SampleBackend",
                Host = context.Request.Host.Value,
                ForwardedFor = context.Request.Headers["X-Forwarded-For"].ToString(),
                ForwardedHost = context.Request.Headers["X-Forwarded-Host"].ToString(),
                ForwardedProto = context.Request.Headers["X-Forwarded-Proto"].ToString(),
            }
        );
    }
);

app.Run();
