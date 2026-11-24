using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ruth.Host.IntegrationTests.Fixtures;

internal sealed class AppConfigurationOnlyHostFactory(
    string environment,
    IReadOnlyDictionary<string, string?> settings,
    Action<IServiceCollection>? configureServices = null
) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _ = builder.UseEnvironment(environment);
        _ = builder.ConfigureAppConfiguration(
            (_, configBuilder) => configBuilder.AddInMemoryCollection(settings)
        );

        if (configureServices is not null)
        {
            _ = builder.ConfigureServices(configureServices);
        }
    }
}
