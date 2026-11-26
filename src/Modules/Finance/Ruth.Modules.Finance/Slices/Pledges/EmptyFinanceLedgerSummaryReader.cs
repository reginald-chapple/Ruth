namespace Ruth.Modules.Finance.Slices.Pledges;

internal sealed class EmptyFinanceLedgerSummaryReader : IFinanceLedgerSummaryReader
{
    public Task<
        IReadOnlyDictionary<(Guid PersonId, Guid AccountId), decimal>
    > GetSettledContributionsAsync(
        IReadOnlyCollection<Guid> personIds,
        IReadOnlyCollection<Guid> accountIds,
        DateOnly minDate,
        DateOnly maxDate,
        CancellationToken cancellationToken = default
    )
    {
        IReadOnlyDictionary<(Guid PersonId, Guid AccountId), decimal> empty =
            new Dictionary<(Guid PersonId, Guid AccountId), decimal>();
        return Task.FromResult(empty);
    }

    public Task<(
        decimal OpenInvoiceBalance,
        decimal LifetimeSettledTotal
    )> GetConstituentTotalsAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult((0m, 0m));
    }
}
