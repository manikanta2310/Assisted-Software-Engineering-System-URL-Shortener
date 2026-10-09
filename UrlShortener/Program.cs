using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using UrlShortener.Api.Middleware;
using UrlShortener.Application;
using UrlShortener.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ── Structured logging (Serilog behind the standard ILogger) ────────────────
builder.Host.UseSerilog((context, configuration) => configuration
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console());

// ── Options (strongly-typed config) ─────────────────────────────────────────
builder.Services.Configure<UrlShortenerOptions>(
    builder.Configuration.GetSection(UrlShortenerOptions.SectionName));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<UrlShortenerOptions>>().Value);

// ── Layers ──────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=urlshortener.db";
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

// Register the real SQLite database only outside the Testing environment. In
// Testing, the integration test factory supplies an in-memory provider, so the
// two providers can never both be registered.
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddSqliteDatabase(connectionString);
}

// ── Web / API ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Centralised error handling → clean ProblemDetails, no leaked internals (S7).
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Health endpoint.
builder.Services.AddHealthChecks();

// Rate limiting on the create endpoint (abuse control, S3).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("create", limiter =>
    {
        limiter.PermitLimit = 20;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

var app = builder.Build();

// ── Database bootstrap ──────────────────────────────────────────────────────
// Prototype uses EnsureCreated for zero-setup runs. In a team/production setting
// this is replaced by EF Core migrations (`dotnet ef migrations add` / Migrate()).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// ── Middleware pipeline ─────────────────────────────────────────────────────
app.UseExceptionHandler();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsProduction())
{
    app.UseHsts();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.UseRateLimiter();

// Swagger UI (/swagger) renders the built-in OpenAPI document. Registered as
// middleware before the endpoints.
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "URL Shortener API v1");
    options.RoutePrefix = "swagger";
});

app.MapControllers();
app.MapHealthChecks("/health");
app.MapOpenApi();

await app.RunAsync();

// Exposed so the integration test project can spin up the real pipeline.
public partial class Program;
