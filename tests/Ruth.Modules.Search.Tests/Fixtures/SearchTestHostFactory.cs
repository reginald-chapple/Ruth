using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.Modules.Search.Infrastructure;
using Ruth.Modules.Search.Infrastructure.Querying;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Search.Tests.Fixtures;

internal static class SearchTestHostFactory
{
    public static async Task<(
        ServiceProvider Provider,
        StubEntityAuthorizationService Auth
    )> CreateAsync(
        string connectionString,
        IReadOnlyList<ISearchDocumentProvider>? extraProviders = null
    )
    {
        var auth = new StubEntityAuthorizationService();
        var provider = BuildServiceProvider(connectionString, auth, extraProviders);
        await InitializeDatabaseAsync(provider);
        return (provider, auth);
    }

    private static ServiceProvider BuildServiceProvider(
        string connectionString,
        StubEntityAuthorizationService auth,
        IReadOnlyList<ISearchDocumentProvider>? extraProviders
    )
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["ConnectionStrings:RuthDatabase"] = connectionString,
                    ["Bootstrap:AutoMigrateAndSeed"] = "false",
                }
            )
            .Build();

        var services = new ServiceCollection();
        _ = services.AddSingleton<IConfiguration>(configuration);
        _ = services.AddLogging(static b => b.SetMinimumLevel(LogLevel.Warning));
        _ = services.AddSharedKernel();
        _ = services.AddRuthPersistence(configuration);
        _ = services.AddSingleton<IEntityAuthorizationService>(auth);
        _ = services.AddSearchModule(configuration);

        if (extraProviders is not null)
        {
            foreach (var p in extraProviders)
            {
                _ = services.AddSingleton(p);
            }
        }

        return services.BuildServiceProvider();
    }

    private static async Task InitializeDatabaseAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var searchDb = scope.ServiceProvider.GetRequiredService<SearchDbContext>();
        await searchDb.Database.MigrateAsync();
        await SearchPostgresIndexInitializer.EnsureFullTextIndexesAsync(
            searchDb,
            CancellationToken.None
        );
    }
}
