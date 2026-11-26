using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Pledges;

/// <summary>Query to list financial pledges with live fulfillment metrics.</summary>
/// <param name="AccountId">Optional filter by financial account identifier.</param>
/// <param name="PersonId">Optional filter by constituent or organization Person identifier.</param>
/// <param name="Status">Optional filter by pledge status.</param>
/// <param name="AsOfDate">Optional reference date for pro-rata pace evaluation (defaults to UTC today).</param>
public sealed record GetFinancialPledgesQuery(
    Guid? AccountId = null,
    Guid? PersonId = null,
    FinancialPledgeStatus? Status = null,
    DateOnly? AsOfDate = null
) : IQuery<IReadOnlyList<FinancialPledgeResponse>>;
