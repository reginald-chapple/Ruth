using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Identity.Contracts.Onboarding;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Host.IntegrationTests.Fixtures;

public sealed class HostWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestProductionJwtSigningKey =
        "ruth-test-only-production-jwt-signing-key-32chars-min";

    public static readonly Guid DefaultWorkspaceTenantGuid = new(
        0x01967000,
        0x0000,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x01
    );

    private readonly string _environment;
    private readonly Dictionary<string, string?> _settings;
    private readonly Action<IServiceCollection>? _configureServices;
    private readonly Func<HttpContext, Func<Task>, Task>? _testRouteInterceptor;

    private HostWebApplicationFactory(
        string environment,
        Dictionary<string, string?> settings,
        Action<IServiceCollection>? configureServices,
        Func<HttpContext, Func<Task>, Task>? testRouteInterceptor
    )
    {
        _environment = environment;
        _settings = settings;
        _configureServices = configureServices;
        _testRouteInterceptor = testRouteInterceptor;
    }

    public static HostWebApplicationFactory Create(
        string postgresConnectionString,
        string redisConnectionString,
        string rustFsEndpoint,
        string environment = "Development",
        IReadOnlyDictionary<string, string?>? overrides = null,
        Action<IServiceCollection>? configureServices = null,
        Func<HttpContext, Func<Task>, Task>? testRouteInterceptor = null
    )
    {
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConnectionStrings:RuthDatabase"] = postgresConnectionString,
            ["ConnectionStrings:Redis"] = redisConnectionString,
            ["Storage:RustFs:Endpoint"] = rustFsEndpoint,
            ["Storage:RustFs:BucketName"] = "ruth-test",
            ["Storage:RustFs:AccessKey"] = "rustfsadmin",
            ["Storage:RustFs:SecretKey"] = "rustfsadmin",
            ["Storage:RustFs:HealthCheckTimeoutSeconds"] = "2",
            ["HealthChecks:TimeoutSeconds"] = "2",
            ["Tenancy:DedicatedWorkspaceTenantId"] = DefaultWorkspaceTenantGuid.ToString("D"),
        };

        if (
            string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase)
            && (overrides is null || !overrides.ContainsKey("Authentication:Jwt:SigningKey"))
        )
        {
            settings["Authentication:Jwt:SigningKey"] = TestProductionJwtSigningKey;
        }

        if (overrides is not null)
        {
            foreach (var (key, value) in overrides)
            {
                settings[key] = value;
            }
        }

        return new HostWebApplicationFactory(
            environment,
            settings,
            configureServices,
            testRouteInterceptor
        );
    }

    public static HostWebApplicationFactory CreateWithOfflineDependencies(
        string environment = "Development",
        IReadOnlyDictionary<string, string?>? overrides = null,
        Action<IServiceCollection>? configureServices = null,
        Func<HttpContext, Func<Task>, Task>? testRouteInterceptor = null,
        bool assumeOnboarded = true
    )
    {
        var deadPgPort = GetUnusedLoopbackPort();
        var deadRedisPort = GetUnusedLoopbackPort();
        var deadRustFsPort = GetUnusedLoopbackPort();

        var combinedConfigure = assumeOnboarded
            ? services => ConfigureOfflineOnboardedServices(services, configureServices)
            : configureServices;

        return Create(
            string.Create(
                CultureInfo.InvariantCulture,
                $"Host=127.0.0.1;Port={deadPgPort};Database=ruth;Username=ruth;Password=ruth_secret;Timeout=1"
            ),
            string.Create(
                CultureInfo.InvariantCulture,
                $"127.0.0.1:{deadRedisPort},abortConnect=false,connectTimeout=500"
            ),
            string.Create(CultureInfo.InvariantCulture, $"http://127.0.0.1:{deadRustFsPort}"),
            environment,
            overrides,
            combinedConfigure,
            testRouteInterceptor
        );
    }

    public HttpClient CreateNoRedirectClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public static int GetUnusedLoopbackPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (
            _settings.TryGetValue("Tenancy:DedicatedWorkspaceTenantId", out var tidRaw)
            && Guid.TryParse(tidRaw, out var tidGuid)
            && tidGuid != Guid.Empty
        )
        {
            AmbientTenantContext.SetTestHarnessDefault(
                TenantId.From(tidGuid),
                TenantLifecycleStage.Live
            );
        }
        else
        {
            AmbientTenantContext.SetTestHarnessDefault(tenantId: null);
        }

        _ = builder.UseEnvironment(_environment);

        foreach (var (key, value) in _settings)
        {
            if (value is not null)
            {
                _ = builder.UseSetting(key, value);
            }
        }

        _ = builder.ConfigureAppConfiguration(
            (_, configBuilder) => configBuilder.AddInMemoryCollection(_settings)
        );

        _ = builder.ConfigureServices(services =>
        {
            _configureServices?.Invoke(services);

            if (_testRouteInterceptor is not null)
            {
                _ = services.AddSingleton<IStartupFilter>(
                    new TestRouteStartupFilter(_testRouteInterceptor)
                );
            }
        });
    }

    private static void ConfigureOfflineOnboardedServices(
        IServiceCollection services,
        Action<IServiceCollection>? configureServices
    )
    {
        _ = services.Replace(
            ServiceDescriptor.Singleton<IOnboardingStateReader>(
                new CompletedOnboardingStateReaderStub()
            )
        );
        configureServices?.Invoke(services);
    }

    private sealed class TestRouteStartupFilter(Func<HttpContext, Func<Task>, Task> interceptor)
        : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                next(app);
                _ = app.Use(interceptor);
            };
        }
    }
}
