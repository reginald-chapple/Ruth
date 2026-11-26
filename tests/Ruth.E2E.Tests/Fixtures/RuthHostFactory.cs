using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Ruth.E2E.Tests.Fixtures;

public sealed class RuthHostFactory : WebApplicationFactory<Program>
{
    public const string DefaultDevJwtSigningKey =
        "replace-with-a-unique-random-32-plus-character-secret-in-production";

    public const string SafeTestJwtSigningKey =
        "ruth-e2e-test-signing-key-at-least-32-characters-long!";

    public const string DefaultWorkspaceTenantId = "01967000-0000-7000-8000-000000000001";

    private readonly string _environment;
    private readonly Dictionary<string, string?> _settings;

    private RuthHostFactory(string environment, Dictionary<string, string?> settings)
    {
        _environment = environment;
        _settings = settings;
    }

    public static RuthHostFactory Create(
        E2EContainersFixture containers,
        string connectionString,
        string environment = "Development",
        string? jwtSigningKey = null,
        IReadOnlyDictionary<string, string?>? overrides = null
    )
    {
        ArgumentNullException.ThrowIfNull(containers);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);

        var resolvedConnectionString = connectionString.Contains('=', StringComparison.Ordinal)
            ? connectionString
            : containers.GetConnectionString(connectionString);

        var settings = BuildBaseSettings(
            resolvedConnectionString,
            containers.RedisConnectionString,
            containers.RustFsEndpoint,
            E2EContainersFixture.RustFsAccessKey,
            E2EContainersFixture.RustFsSecretKey,
            jwtSigningKey,
            overrides
        );

        return new RuthHostFactory(environment, settings);
    }

    public static RuthHostFactory CreateWithCustomEndpoints(
        string postgresConnectionString,
        string redisConnectionString,
        string rustFsEndpoint,
        string environment = "Development",
        string? jwtSigningKey = null,
        IReadOnlyDictionary<string, string?>? overrides = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(postgresConnectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(redisConnectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(rustFsEndpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);

        var settings = BuildBaseSettings(
            postgresConnectionString,
            redisConnectionString,
            rustFsEndpoint,
            E2EContainersFixture.RustFsAccessKey,
            E2EContainersFixture.RustFsSecretKey,
            jwtSigningKey,
            overrides
        );

        return new RuthHostFactory(environment, settings);
    }

    public HttpClient CreateNoRedirectClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public static ActivityCapture StartActivityCapture(params string[] sourcePrefixes)
    {
        return new ActivityCapture(sourcePrefixes);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _ = builder.UseEnvironment(_environment);

        if (!string.Equals(_environment, "Development", StringComparison.OrdinalIgnoreCase))
        {
            _ = builder.UseStaticWebAssets();
        }

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
    }

    private static Dictionary<string, string?> BuildBaseSettings(
        string postgresConnectionString,
        string redisConnectionString,
        string rustFsEndpoint,
        string rustFsAccessKey,
        string rustFsSecretKey,
        string? jwtSigningKey,
        IReadOnlyDictionary<string, string?>? overrides
    )
    {
        var settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ConnectionStrings:RuthDatabase"] = postgresConnectionString,
            ["ConnectionStrings:Redis"] = redisConnectionString,
            ["ConnectionStrings:RustFs"] = rustFsEndpoint,
            ["Storage:RustFs:Endpoint"] = rustFsEndpoint,
            ["Storage:RustFs:BucketName"] = "ruth-e2e",
            ["Storage:RustFs:AccessKey"] = rustFsAccessKey,
            ["Storage:RustFs:SecretKey"] = rustFsSecretKey,
            ["Storage:RustFs:HealthCheckTimeoutSeconds"] = "2",
            ["HealthChecks:TimeoutSeconds"] = "2",
            ["Storage:Endpoint"] = rustFsEndpoint,
            ["Storage:ServiceUrl"] = rustFsEndpoint,
            ["Storage:AccessKey"] = rustFsAccessKey,
            ["Storage:SecretKey"] = rustFsSecretKey,
            ["Storage:BucketName"] = "ruth-e2e",
            ["Storage:UseSsl"] = "false",
            ["RustFs:Endpoint"] = rustFsEndpoint,
            ["RustFs:AccessKey"] = rustFsAccessKey,
            ["RustFs:SecretKey"] = rustFsSecretKey,
            ["Authentication:Jwt:SigningKey"] = jwtSigningKey ?? SafeTestJwtSigningKey,
            ["Bootstrap:AutoMigrateAndSeed"] = "true",
            ["Tenancy:DedicatedWorkspaceTenantId"] = DefaultWorkspaceTenantId,
        };

        if (overrides is null)
        {
            return settings;
        }

        foreach (var (key, value) in overrides)
        {
            settings[key] = value;
        }

        return settings;
    }
}
