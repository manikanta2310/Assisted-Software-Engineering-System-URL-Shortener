using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UrlShortener.Infrastructure.Persistence;

namespace UrlShortener.IntegrationTests;

/// <summary>
/// Boots the real application pipeline with an in-memory EF Core database.
/// Program.cs skips registering SQLite in the "Testing" environment, so this
/// factory simply supplies the in-memory provider — only one EF Core provider
/// is ever registered, avoiding the "single database provider" conflict.
/// </summary>
public sealed class CustomWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Each factory instance gets its own isolated in-memory database.
            var dbName = "it-" + Guid.NewGuid();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));
        });
    }
}
