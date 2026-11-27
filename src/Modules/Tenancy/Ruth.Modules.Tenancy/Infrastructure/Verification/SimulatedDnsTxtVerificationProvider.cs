using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Ruth.Modules.Tenancy.Contracts.Verification;

namespace Ruth.Modules.Tenancy.Infrastructure.Verification;

/// <summary>
/// Deterministic DNS TXT verification provider supporting staged TXT records and mock-mode
/// verification challenges while blocking unverified caller self-attestation when mock mode is disabled.
/// </summary>
public sealed class SimulatedDnsTxtVerificationProvider(IOptions<TenancyOptions> options)
    : IDnsTxtVerificationProvider
{
    private readonly TenancyOptions _options = options?.Value
        ?? throw new ArgumentNullException(nameof(options));

    private readonly ConcurrentDictionary<string, string[]> _stagedRecords = new(
        StringComparer.OrdinalIgnoreCase
    );

    /// <inheritdoc />
    public bool IsMockMode => _options.DnsMockMode;

    /// <summary>Stages an authoritative DNS TXT record value for deterministic verification testing.</summary>
    public void StageTxtRecord(string txtRecordName, string txtValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(txtRecordName);
        ArgumentException.ThrowIfNullOrWhiteSpace(txtValue);

        var key = txtRecordName.Trim().TrimEnd('.').ToLowerInvariant();
        var value = txtValue.Trim();
        _ = _stagedRecords.AddOrUpdate(
            key,
            static (_, val) => [val],
            static (_, existing, val) =>
                existing.Contains(val, StringComparer.Ordinal) ? existing : [.. existing, val],
            value
        );
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ResolveTxtRecordsAsync(
        string txtRecordName,
        string? simulatedObservedToken = null,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(txtRecordName))
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        var key = txtRecordName.Trim().TrimEnd('.').ToLowerInvariant();
        var results = new List<string>(2);
        if (_stagedRecords.TryGetValue(key, out var staged))
        {
            results.AddRange(staged);
        }

        if (IsMockMode && !string.IsNullOrWhiteSpace(simulatedObservedToken))
        {
            var token = simulatedObservedToken.Trim();
            if (!results.Contains(token, StringComparer.Ordinal))
            {
                results.Add(token);
            }
        }

        return Task.FromResult<IReadOnlyList<string>>(results);
    }
}
