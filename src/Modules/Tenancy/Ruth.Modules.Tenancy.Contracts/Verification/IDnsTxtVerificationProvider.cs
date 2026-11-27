namespace Ruth.Modules.Tenancy.Contracts.Verification;

/// <summary>Resolves DNS TXT records to verify domain control before activating a custom organization hostname.</summary>
public interface IDnsTxtVerificationProvider
{
    /// <summary>Gets whether DNS TXT verification runs in deterministic zero-config mock mode.</summary>
    bool IsMockMode { get; }

    /// <summary>Resolves TXT record values published at <paramref name="txtRecordName" />.</summary>
    Task<IReadOnlyList<string>> ResolveTxtRecordsAsync(
        string txtRecordName,
        string? simulatedObservedToken = null,
        CancellationToken cancellationToken = default
    );
}
