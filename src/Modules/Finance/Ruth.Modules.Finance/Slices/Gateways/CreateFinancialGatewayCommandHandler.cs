using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Gateways;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Infrastructure.Gateways;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Gateways;

internal sealed class CreateFinancialGatewayCommandHandler(
    FinanceDbContext dbContext,
    IPaymentGatewayRegistry registry
) : ICommandHandler<CreateFinancialGatewayCommand, FinancialGatewayResponse>
{
    public async Task<Result<FinancialGatewayResponse>> HandleAsync(
        CreateFinancialGatewayCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Spec is null)
        {
            return FinanceErrors.GatewayNameInvalid;
        }

        var created = FinancialGateway.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<FinancialGatewayResponse>(created.Errors);
        }

        _ = dbContext.FinancialGateways.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return FinancialGatewayMapper.ToResponse(created.Value, registry);
    }
}
