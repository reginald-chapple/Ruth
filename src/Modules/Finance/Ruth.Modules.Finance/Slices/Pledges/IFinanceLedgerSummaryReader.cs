namespace Ruth.Modules.Finance.Slices.Pledges;

internal interface IFinanceLedgerSummaryReader
{
    Task<
        IReadOnlyDictionary<(Guid PersonId, Guid AccountId), decimal>
    > GetSettledContributionsAsync(
        IReadOnlyCollection<Guid> personIds,
        IReadOnlyCollection<Guid> accountIds,
        DateOnly minDate,
        DateOnly maxDate,
        CancellationToken cancellationToken = default
    );

    Task<(decimal OpenInvoiceBalance, decimal LifetimeSettledTotal)> GetConstituentTotalsAsync(
        Guid personId,
        CancellationToken cancellationToken = default
    );
}
