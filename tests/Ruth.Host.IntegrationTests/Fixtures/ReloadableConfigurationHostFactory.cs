using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ruth.Host.IntegrationTests.Fixtures;

internal sealed class ReloadableConfigurationHostFactory(
    string environment,
    ReloadableMemoryConfigurationSource source
) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _ = builder.UseEnvironment(environment);
        _ = builder.ConfigureAppConfiguration((_, configBuilder) => configBuilder.Add(source));
    }
}
