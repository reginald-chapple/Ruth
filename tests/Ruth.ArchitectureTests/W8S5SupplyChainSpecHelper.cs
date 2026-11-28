using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace Ruth.ArchitectureTests;

internal static class W8S5SupplyChainSpecHelper
{
    public static string LocateRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Ruth.slnx")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException("Unable to locate Ruth.slnx repository root.");
    }

    public static void AssertAllDockerfileFromLinesPinnedBySha256(string dockerfileContent)
    {
        var fromLines = dockerfileContent
            .Split('\n')
            .Select(static l => l.Trim())
            .Where(static l => l.StartsWith("FROM ", StringComparison.Ordinal))
            .ToArray();

        Assert.NotEmpty(fromLines);
        foreach (var line in fromLines)
        {
            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var imageRef = tokens[1];
            Assert.DoesNotContain(":latest", imageRef, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("@sha256:", imageRef, StringComparison.Ordinal);
        }
    }

    public static void AssertAllWorkflowActionsPinnedToFullCommitSha(string workflowYaml)
    {
        var usesRefs = new List<string>();
        foreach (var rawLine in workflowYaml.Split('\n'))
        {
            var trimmed = rawLine.Trim();
            if (trimmed.StartsWith("- uses:", StringComparison.Ordinal))
            {
                trimmed = trimmed["- uses:".Length..].Trim();
            }
            else if (trimmed.StartsWith("uses:", StringComparison.Ordinal))
            {
                trimmed = trimmed["uses:".Length..].Trim();
            }
            else
            {
                continue;
            }

            var commentIdx = trimmed.IndexOf('#', StringComparison.Ordinal);
            var actionRef = (commentIdx >= 0 ? trimmed[..commentIdx] : trimmed).Trim();
            usesRefs.Add(actionRef);
        }

        Assert.NotEmpty(usesRefs);
        foreach (var actionRef in usesRefs)
        {
            Assert.True(
                IsFortyHexPinnedAction(actionRef),
                $"Unpinned GitHub Action reference detected in ci.yml: '{actionRef}'"
            );
        }
    }

    public static void AssertZeroLeakedSecretsAcrossSourceAndDeployTrees(string repoRoot)
    {
        string[] scanRoots = ["src", "deploy", ".github", "scripts", "docs"];
        foreach (var relRoot in scanRoots)
        {
            var fullDir = Path.Combine(repoRoot, relRoot);
            if (!Directory.Exists(fullDir))
            {
                continue;
            }

            foreach (
                var file in Directory.EnumerateFiles(fullDir, "*", SearchOption.AllDirectories)
            )
            {
                if (IsBinaryBuildArtifact(file))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                Assert.False(
                    ContainsLeakedProviderToken(text),
                    $"Potential leaked credential token detected in {Path.GetRelativePath(repoRoot, file)}"
                );
            }
        }
    }

    public static void AssertSecurityDocCitedFilesExist(string repoRoot, string securityMarkdown)
    {
        var citedPaths = ExtractBacktickRepoPaths(securityMarkdown);
        Assert.NotEmpty(citedPaths);
        foreach (var relPath in citedPaths)
        {
            var fullPath = Path.Combine(
                repoRoot,
                relPath.Replace('/', Path.DirectorySeparatorChar)
            );
            Assert.True(
                File.Exists(fullPath) || Directory.Exists(fullPath),
                $"SECURITY.md cites missing repository path: {relPath}"
            );
        }
    }

    public static async Task<string> GenerateCycloneDxSbomAsync(
        string repoRoot,
        string outputRelativePath
    )
    {
        var scriptPath = Path.Combine(repoRoot, "scripts", "generate-sbom.mjs");
        var psi = new ProcessStartInfo("node")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add(scriptPath);
        psi.ArgumentList.Add(outputRelativePath);

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to launch node for SBOM generation.");
        var stderr = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();
        Assert.True(
            proc.ExitCode == 0,
            string.Create(
                CultureInfo.InvariantCulture,
                $"SBOM generator exited with {proc.ExitCode}: {stderr}"
            )
        );

        return await File.ReadAllTextAsync(Path.Combine(repoRoot, outputRelativePath));
    }

    private static bool IsFortyHexPinnedAction(string actionRef)
    {
        var at = actionRef.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0 || !actionRef[..at].Contains('/', StringComparison.Ordinal))
        {
            return false;
        }

        var sha = actionRef[(at + 1)..];
        if (sha.Length != 40)
        {
            return false;
        }

        foreach (var ch in sha)
        {
            if (ch is not (>= '0' and <= '9') and not (>= 'a' and <= 'f'))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ContainsLeakedProviderToken(string text)
    {
        return text.Contains("-----BEGIN PRIVATE KEY-----", StringComparison.Ordinal)
            || text.Contains("-----BEGIN RSA PRIVATE KEY-----", StringComparison.Ordinal)
            || text.Contains("-----BEGIN OPENSSH PRIVATE KEY-----", StringComparison.Ordinal)
            || HasTokenPrefixWithAlphaNumRun(text, "AKIA", 16)
            || HasTokenPrefixWithAlphaNumRun(text, "ghp_", 36)
            || HasTokenPrefixWithAlphaNumRun(text, "sk_live_", 24);
    }

    private static bool HasTokenPrefixWithAlphaNumRun(string text, string prefix, int minTailChars)
    {
        var idx = text.IndexOf(prefix, StringComparison.Ordinal);
        while (idx >= 0)
        {
            var start = idx + prefix.Length;
            var count = 0;
            while (start + count < text.Length && char.IsAsciiLetterOrDigit(text[start + count]))
            {
                count++;
            }

            if (count >= minTailChars)
            {
                return true;
            }

            idx = text.IndexOf(prefix, start, StringComparison.Ordinal);
        }

        return false;
    }

    private static List<string> ExtractBacktickRepoPaths(string markdown)
    {
        string[] prefixes = ["src/", "tests/", "deploy/", ".github/", "scripts/"];
        var list = new List<string>();
        var parts = markdown.Split('`');
        for (var i = 1; i < parts.Length; i += 2)
        {
            var candidate = parts[i].Trim();
            if (
                !candidate.Contains(' ', StringComparison.Ordinal)
                && prefixes.Any(p => candidate.StartsWith(p, StringComparison.Ordinal))
            )
            {
                list.Add(candidate);
            }
        }

        return list;
    }

    private static bool IsBinaryBuildArtifact(string filePath)
    {
        return filePath.Contains(
                $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase
            )
            || filePath.Contains(
                $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                StringComparison.OrdinalIgnoreCase
            );
    }
}
