using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using UrlShortener.Application.Interfaces;
using UrlShortener.Application.Services;
using UrlShortener.Application.Validation;
using UrlShortener.Infrastructure.Persistence;
using UrlShortener.Infrastructure.Repositories;
using UrlShortener.Infrastructure.Services;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Service registration for the whole app, in one place. Grouped into two methods
/// so the composition root reads clearly, but kept in a single file because this
/// is one project.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUrlService, UrlService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddValidatorsFromAssemblyContaining<CreateShortUrlRequestValidator>();
        return services;
    }

    /// <summary>
    /// Registers infrastructure services EXCEPT the database. The DbContext is
    /// registered separately (see AddSqliteDatabase) so tests can substitute an
    /// in-memory provider without two providers ever coexisting.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddMemoryCache();

        // Repository + cache decorator: resolve the concrete repo, then wrap it so
        // callers get the cached version through the interface.
        services.AddScoped<UrlRepository>();
        services.AddScoped<IUrlRepository>(sp => new CachedUrlRepository(
            sp.GetRequiredService<UrlRepository>(),
            sp.GetRequiredService<IMemoryCache>()));

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ICodeGenerator, Base62CodeGenerator>();
        services.AddSingleton<IClickTracker, ClickTracker>();

        return services;
    }

    /// <summary>Registers the real SQLite database. Not called in the Testing
    /// environment — the integration test factory adds an in-memory provider
    /// instead, so only ever one EF Core provider is registered.</summary>
    public static IServiceCollection AddSqliteDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        return services;
    }
}
