namespace Ruth.Modules.Crm.Contracts.Duplicates;

/// <summary>Canonical person resolution outcome from the PersonRedirectLedger.</summary>
public sealed record CanonicalPersonResolutionResponse(
    Guid RequestedId,
    Guid CanonicalPersonId,
    bool WasRedirected,
    int RedirectHops
);
