using System.Text.Json;
using Ruth.Host.Health;
using Xunit;

namespace Ruth.ArchitectureTests;

public sealed class W8S5ContainerCiAndSupplyChainVerificationTests
{
    [Fact]
    public void HostAndPostgresDockerfiles_EnforceSha256PinnedChiseledNonRootAndHealthcheckPosture()
    {
        var root = W8S5SupplyChainSpecHelper.LocateRepoRoot();
        var hostDockerfile = File.ReadAllText(
            Path.Combine(root, "deploy", "docker", "dotnet", "Dockerfile")
        );
        var pgDockerfile = File.ReadAllText(
            Path.Combine(root, "deploy", "docker", "postgres", "Dockerfile")
        );

        W8S5SupplyChainSpecHelper.AssertAllDockerfileFromLinesPinnedBySha256(hostDockerfile);
        W8S5SupplyChainSpecHelper.AssertAllDockerfileFromLinesPinnedBySha256(pgDockerfile);

        Assert.Contains("chiseled", hostDockerfile, StringComparison.Ordinal);
        Assert.Contains("DOTNET_EnableDiagnostics=0", hostDockerfile, StringComparison.Ordinal);
        Assert.Contains("USER $APP_UID", hostDockerfile, StringComparison.Ordinal);
        Assert.Contains("HEALTHCHECK", hostDockerfile, StringComparison.Ordinal);
        Assert.Contains("--healthcheck", hostDockerfile, StringComparison.Ordinal);
        Assert.Contains("USER postgres", pgDockerfile, StringComparison.Ordinal);
        Assert.Contains("HEALTHCHECK", pgDockerfile, StringComparison.Ordinal);
    }

    [Fact]
    public void DockerCompose_EnforcesLoopbackPorts_InternalBackingNetwork_AuthenticatedRedis_PinnedDigests_AndDroppedCaps()
    {
        var root = W8S5SupplyChainSpecHelper.LocateRepoRoot();
        var compose = File.ReadAllText(Path.Combine(root, "docker-compose.yml"));

        Assert.DoesNotContain(":latest", compose, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("redis:8-alpine@sha256:", compose, StringComparison.Ordinal);
        Assert.Contains("rustfs/rustfs:1.0.7@sha256:", compose, StringComparison.Ordinal);
        Assert.Contains("--requirepass", compose, StringComparison.Ordinal);
        Assert.Contains("internal: true", compose, StringComparison.Ordinal);
        Assert.Contains("user: \"1654:1654\"", compose, StringComparison.Ordinal);
        Assert.Contains("read_only: true", compose, StringComparison.Ordinal);
        Assert.Contains("cap_drop:", compose, StringComparison.Ordinal);
        Assert.Contains("- ALL", compose, StringComparison.Ordinal);
        Assert.Contains("no-new-privileges:true", compose, StringComparison.Ordinal);

        foreach (var rawLine in compose.Split('\n'))
        {
            var trimmed = rawLine.Trim();
            if (
                trimmed.StartsWith("- \"", StringComparison.Ordinal)
                && trimmed.Contains(':', StringComparison.Ordinal)
            )
            {
                Assert.StartsWith("- \"127.0.0.1:", trimmed, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void CiWorkflow_EnforcesReadOnlyPermissions_100PercentShaPinnedActions_AndAllSupplyChainGates()
    {
        var root = W8S5SupplyChainSpecHelper.LocateRepoRoot();
        var ci = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));
        var gitleaksToml = File.ReadAllText(Path.Combine(root, ".gitleaks.toml"));

        Assert.Contains("contents: read", ci, StringComparison.Ordinal);
        W8S5SupplyChainSpecHelper.AssertAllWorkflowActionsPinnedToFullCommitSha(ci);
        Assert.Contains("gitleaks/gitleaks-action@", ci, StringComparison.Ordinal);
        Assert.Contains("package --vulnerable --include-transitive", ci, StringComparison.Ordinal);
        Assert.Contains("npm audit --audit-level=high", ci, StringComparison.Ordinal);
        Assert.Contains("aquasecurity/trivy-action@", ci, StringComparison.Ordinal);
        Assert.Contains("scan-type: \"config\"", ci, StringComparison.Ordinal);
        Assert.Contains("npm run sbom:generate", ci, StringComparison.Ordinal);
        Assert.Contains("useDefault = true", gitleaksToml, StringComparison.Ordinal);

        W8S5SupplyChainSpecHelper.AssertZeroLeakedSecretsAcrossSourceAndDeployTrees(root);
    }

    [Fact]
    public async Task CycloneDxSbomGenerator_IsByteForByteReproducible_AndInventoriesNuGetNpmAndOciImages()
    {
        var root = W8S5SupplyChainSpecHelper.LocateRepoRoot();
        var first = await W8S5SupplyChainSpecHelper.GenerateCycloneDxSbomAsync(
            root,
            "artifacts/sbom/test-run-1.cdx.json"
        );
        var second = await W8S5SupplyChainSpecHelper.GenerateCycloneDxSbomAsync(
            root,
            "artifacts/sbom/test-run-2.cdx.json"
        );

        Assert.Equal(first, second);

        using var doc = JsonDocument.Parse(first);
        Assert.Equal("CycloneDX", doc.RootElement.GetProperty("bomFormat").GetString());
        Assert.Equal("1.6", doc.RootElement.GetProperty("specVersion").GetString());

        var purls = doc
            .RootElement.GetProperty("components")
            .EnumerateArray()
            .Select(static c => c.GetProperty("purl").GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(purls, static p =>
            p.StartsWith("pkg:nuget/Npgsql@", StringComparison.Ordinal)
        );
        Assert.Contains(purls, static p =>
            p.StartsWith("pkg:nuget/csharpier@", StringComparison.Ordinal)
        );
        Assert.Contains(purls, static p =>
            p.StartsWith("pkg:npm/%40biomejs/biome@", StringComparison.Ordinal)
        );
        Assert.Contains(purls, static p =>
            p.Contains("dotnet/aspnet@sha256:", StringComparison.Ordinal)
        );
        Assert.Contains(purls, static p =>
            p.Contains("rustfs/rustfs@sha256:", StringComparison.Ordinal)
        );
    }

    [Fact]
    public void SecurityMarkdownAndContainerHealthProbe_VerifyDocumentedPathsAndCliParsing()
    {
        var root = W8S5SupplyChainSpecHelper.LocateRepoRoot();
        var securityDoc = File.ReadAllText(Path.Combine(root, "docs", "SECURITY.md"));

        Assert.Contains(
            "SaaS Isolation & Verifiable Controls Matrix",
            securityDoc,
            StringComparison.Ordinal
        );
        Assert.Contains("3-2-1-1-0", securityDoc, StringComparison.Ordinal);
        Assert.Contains("Object Lock", securityDoc, StringComparison.Ordinal);
        Assert.Contains("restic check", securityDoc, StringComparison.Ordinal);
        W8S5SupplyChainSpecHelper.AssertSecurityDocCitedFilesExist(root, securityDoc);

        Assert.True(HostContainerHealthProbe.IsHealthCheckInvocation(["--healthcheck"]));
        Assert.False(HostContainerHealthProbe.IsHealthCheckInvocation([]));
        Assert.Equal(8080, HostContainerHealthProbe.ResolveListeningPort("http://+:8080"));
        Assert.Equal(
            9090,
            HostContainerHealthProbe.ResolveListeningPort("http://*:9090;https://+:9443")
        );
    }
}
