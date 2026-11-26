namespace Ruth.Modules.Tenancy.Contracts.Hosts;

/// <summary>Resolves incoming HTTP hostnames to isolated tenant workspaces or platform control-plane hosts.</summary>
public interface ITenantHostResolver
{
    /// <summary>Resolves <paramref name="hostname" /> against configured platform hosts and verified tenant domains.</summary>
    ValueTask<ResolvedHostResponse> ResolveHostAsync(
        string hostname,
        CancellationToken cancellationToken = default
    );

    /// <summary>Evicts cached resolution entries for <paramref name="hostname" /> (or all entries when <see langword="null" />).</summary>
    void InvalidateHost(string? hostname = null);
}
