using System.Text;
using Microsoft.Extensions.Configuration;
using Ruth.Host.Configuration;
using Xunit;

namespace Ruth.Host.IntegrationTests;

public sealed class ConfigurationValueResolverChainedTests
{
    [Fact]
    public void ConfigurationValueResolver_ChainedInMemoryNullOverride_ShadowsOuterValue()
    {
        var innerConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Storage:RustFs:Endpoint"] = null,
                    ["Observability:OtlpEndpoint"] = null,
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
    public void ConfigurationValueResolver_ChainedJsonNullOverride_ShadowsOuterValue()
    {
        // lang=json
        const string json =
            """{"Storage":{"RustFs":{"Endpoint":null}},"Observability":{"OtlpEndpoint":null}}""";
        using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var innerConfig = new ConfigurationBuilder().AddJsonStream(jsonStream).Build();

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
    public void ConfigurationValueResolver_NestedChainedEmptyOverride_ShadowsMidTierAndAllowsOuterFallback()
    {
        var deepInner = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Storage:RustFs:Endpoint"] = string.Empty,
                    ["Observability:OtlpEndpoint"] = "   ",
                }
            )
            .Build();

        var midInner = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Storage:RustFs:Endpoint"] = "http://mid:9000",
                    ["Observability:OtlpEndpoint"] = "http://mid-otel:4317",
                }
            )
            .AddConfiguration(deepInner)
            .Build();

        var topOuter = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Storage:RustFs:Endpoint"] = "http://top:9000",
                    ["Storage:ServiceUrl"] = "http://top-serviceurl:9000",
                    ["Observability:OtlpEndpoint"] = "http://top-otel:4317",
                }
            )
            .AddConfiguration(midInner)
            .Build();

        Assert.Equal("http://top-serviceurl:9000", RustFsOptions.ResolveEndpoint(topOuter));
        Assert.Null(ObservabilityOptions.ResolveOtlpEndpoint(topOuter));
    }

    [Fact]
    public void ConfigurationValueResolver_ConfigurationManagerWithNestedChain_ReturnsInnerWinningFallback()
    {
        var deepInner = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Storage:RustFs:Endpoint"] = string.Empty,
                }
            )
            .Build();

        var midInner = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Storage:RustFs:Endpoint"] = "http://mid:9000",
                    ["Storage:Endpoint"] = "  http://mid-fallback:9000  ",
                }
            )
            .AddConfiguration(deepInner)
            .Build();

        using var manager = new ConfigurationManager();
        _ = manager
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Storage:RustFs:Endpoint"] = "http://outer:9000",
                    ["Storage:Endpoint"] = "http://outer-fallback:9000",
                }
            )
            .AddConfiguration(midInner);

        Assert.Equal("http://mid-fallback:9000", RustFsOptions.ResolveEndpoint(manager));
    }

    [Fact]
    public void ConfigurationValueResolver_ChainedSectionSource_ResolvesValueAndShadowsEmptyOverride()
    {
        var sourceRoot = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Sub:Storage:RustFs:Endpoint"] = string.Empty,
                    ["Sub:Storage:Endpoint"] = "   ",
                    ["Sub:Observability:OtlpEndpoint"] = "  http://section-chained-otel:4317  ",
                }
            )
            .Build();

        var outerConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Storage:RustFs:Endpoint"] = "http://outer-stale:9000",
                    ["Storage:Endpoint"] = "http://outer-stale-fallback:9000",
                    ["Storage:ServiceUrl"] = "http://outer-serviceurl:9000",
                    ["Observability:OtlpEndpoint"] = "http://outer-stale-otel:4317",
                }
            )
            .AddConfiguration(sourceRoot.GetSection("Sub"))
            .Build();

        Assert.Equal("http://outer-serviceurl:9000", RustFsOptions.ResolveEndpoint(outerConfig));
        Assert.Equal(
            "http://section-chained-otel:4317",
            ObservabilityOptions.ResolveOtlpEndpoint(outerConfig)
        );
    }

    [Fact]
    public void ConfigurationValueResolver_ChainedSectionNullAndSubObject_ShadowsOnlyExplicitNullLeaves()
    {
        var sourceRoot = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Sub:Storage:Endpoint"] = null,
                    ["Sub:Storage:ServiceUrl:Host"] = "ignored-nested-object",
                    ["Sub:OTEL_EXPORTER_OTLP_ENDPOINT"] = null,
                }
            )
            .Build();

        var outerConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Storage:RustFs:Endpoint"] = "http://outer-stale:9000",
                    ["Storage:Endpoint"] = "http://outer-stale-endpoint:9000",
                    ["Storage:ServiceUrl"] = "http://outer-serviceurl:9000",
                    ["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://outer-stale-otel:4317",
                }
            )
            .AddConfiguration(sourceRoot.GetSection("Sub"))
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Storage:RustFs:Endpoint"] = string.Empty,
                }
            )
            .Build();

        Assert.Equal("http://outer-serviceurl:9000", RustFsOptions.ResolveEndpoint(outerConfig));
        Assert.Null(ObservabilityOptions.ResolveOtlpEndpoint(outerConfig));
    }

    [Fact]
    public void ConfigurationValueResolver_DiamondSharedRoot_VisitsSharedRootOnceAndRespectsShadowing()
    {
        var sharedRoot = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Storage:RustFs:Endpoint"] = string.Empty,
                }
            )
            .Build();
        var branchLow = new ConfigurationBuilder().AddConfiguration(sharedRoot).Build();
        var branchHigh = new ConfigurationBuilder().AddConfiguration(sharedRoot).Build();

        var outerConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Storage:RustFs:Endpoint"] = "http://outer-stale:9000",
                    ["Storage:Endpoint"] = "http://outer-fallback:9000",
                }
            )
            .AddConfiguration(branchLow)
            .AddConfiguration(branchHigh)
            .Build();

        Assert.Equal("http://outer-fallback:9000", RustFsOptions.ResolveEndpoint(outerConfig));
    }
}
