using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Gateways;

/// <summary>Query to list compiled payment gateway providers registered in the host.</summary>
public sealed record GetRegisteredPaymentGatewayProvidersQuery
    : IQuery<IReadOnlyList<RegisteredPaymentGatewayProviderResponse>>;
