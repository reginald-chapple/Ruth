using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Gateways;

/// <summary>Command to create a new financial payment gateway configuration.</summary>
/// <param name="Spec">Gateway configuration specification.</param>
public sealed record CreateFinancialGatewayCommand(FinancialGatewaySpec Spec)
    : ICommand<FinancialGatewayResponse>;
