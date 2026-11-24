using Microsoft.Extensions.Configuration;
using Ruth.Host.Configuration;
using Xunit;

namespace Ruth.Host.IntegrationTests;

public sealed class ConfigurationValueResolverUnitTests
{
    [Fact]
    public void ConfigurationValueResolver_SupportsSectionFallbackAndReturnsNullWhenUnconfigured()
    {
        var populatedRoot = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Section:Storage:Endpoint"] = "  http://section-rustfs:9000  ",
                    ["Section:Observability:OtlpEndpoint"] = "  http://section-otel:4317  ",
                }
            )
            .Build();
        var section = populatedRoot.GetSection("Section");

        Assert.Equal("http://section-rustfs:9000", RustFsOptions.ResolveEndpoint(section));
        Assert.Equal("http://section-otel:4317", ObservabilityOptions.ResolveOtlpEndpoint(section));

        var emptyRoot = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Storage:RustFs:Endpoint"] = "   ",
                }
            )
            .Build();

        Assert.Null(RustFsOptions.ResolveEndpoint(emptyRoot));
        Assert.Null(ObservabilityOptions.ResolveOtlpEndpoint(emptyRoot.GetSection("Empty")));
    }

    [Fact]
    public void ConfigurationValueResolver_HigherPriorityEmptyOverride_ShadowsLowerPriorityValue()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Observability:OtlpEndpoint"] = "http://stale-collector:4317",
                    ["Storage:RustFs:Endpoint"] = "http://stale-rustfs:9000",
                }
            )
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Observability:OtlpEndpoint"] = string.Empty,
                    ["Storage:RustFs:Endpoint"] = "   ",
                }
            )
            .Build();

        Assert.Null(ObservabilityOptions.ResolveOtlpEndpoint(config));
        Assert.Null(RustFsOptions.ResolveEndpoint(config));
    }

    [Fact]
    public void ConfigurationValueResolver_HigherPriorityEmptyOverride_AllowsUnshadowedFallbackKey()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Storage:RustFs:Endpoint"] = "http://stale-rustfs:9000",
                    ["Storage:Endpoint"] = "http://fallback-rustfs:9000",
                    ["Observability:OtlpEndpoint"] = "http://stale-collector:4317",
                    ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://fallback-otel:4317",
                }
            )
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Storage:RustFs:Endpoint"] = string.Empty,
                    ["Observability:OtlpEndpoint"] = string.Empty,
                }
            )
            .Build();

        Assert.Equal("http://fallback-rustfs:9000", RustFsOptions.ResolveEndpoint(config));
        Assert.Equal("http://fallback-otel:4317", ObservabilityOptions.ResolveOtlpEndpoint(config));
    }

    [Fact]
    public void ConfigurationValueResolver_ChainedConfigurationProviderEmptyOverride_ShadowsOuterValue()
    {
        var innerConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Storage:RustFs:Endpoint"] = "http://inner-stale:9000",
                    ["Observability:OtlpEndpoint"] = "http://inner-otel:4317",
                }
            )
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Storage:RustFs:Endpoint"] = string.Empty,
                    ["Observability:OtlpEndpoint"] = "   ",
                }
            )
            .Build();

        var outerConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Storage:RustFs:Endpoint"] = "http://outer-stale:9000",
                    ["Observability:OtlpEndpoint"] = "http://outer-otel:4317",
                }
            )
            .AddConfiguration(innerConfig)
            .Build();

        Assert.Null(RustFsOptions.ResolveEndpoint(outerConfig));
        Assert.Null(ObservabilityOptions.ResolveOtlpEndpoint(outerConfig));
    }

    [Fact]
    public void ConfigurationValueResolver_ChainedConfigurationProviderEmptyOverride_PreservesUnshadowedFallback()
    {
        var innerConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Storage:RustFs:Endpoint"] = string.Empty,
                    ["Observability:OtlpEndpoint"] = string.Empty,
                }
            )
            .Build();

        var outerConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Storage:RustFs:Endpoint"] = "http://outer-stale:9000",
                    ["Storage:Endpoint"] = "http://outer-fallback:9000",
                    ["Observability:OtlpEndpoint"] = "http://outer-otel:4317",
                    ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://outer-otel-fallback:4317",
                }
            )
            .AddConfiguration(innerConfig)
            .Build();

        Assert.Equal("http://outer-fallback:9000", RustFsOptions.ResolveEndpoint(outerConfig));
        Assert.Equal(
            "http://outer-otel-fallback:4317",
            ObservabilityOptions.ResolveOtlpEndpoint(outerConfig)
        );
    }
}
