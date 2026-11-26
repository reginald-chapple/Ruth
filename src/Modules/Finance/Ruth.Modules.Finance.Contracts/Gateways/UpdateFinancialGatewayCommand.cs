using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Gateways;

/// <summary>Command to update an existing financial payment gateway configuration.</summary>
/// <param name="GatewayId">Target gateway identifier.</param>
/// <param name="Spec">Updated gateway configuration specification.</param>
public sealed record UpdateFinancialGatewayCommand(Guid GatewayId, FinancialGatewaySpec Spec)
    : ICommand<FinancialGatewayResponse>;
