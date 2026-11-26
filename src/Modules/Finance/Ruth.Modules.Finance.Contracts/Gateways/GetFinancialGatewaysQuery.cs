using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Gateways;

/// <summary>Query to list configured financial payment gateways.</summary>
/// <param name="ActiveOnly">When true, returns only active gateways.</param>
public sealed record GetFinancialGatewaysQuery(bool ActiveOnly = false)
    : IQuery<IReadOnlyList<FinancialGatewayResponse>>;
