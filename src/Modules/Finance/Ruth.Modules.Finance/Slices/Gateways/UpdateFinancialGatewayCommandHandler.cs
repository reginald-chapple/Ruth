using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.Gateways;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.Modules.Finance.Infrastructure.Gateways;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.Gateways;

internal sealed class UpdateFinancialGatewayCommandHandler(
    FinanceDbContext dbContext,
    IPaymentGatewayRegistry registry
) : ICommandHandler<UpdateFinancialGatewayCommand, FinancialGatewayResponse>
{
    public async Task<Result<FinancialGatewayResponse>> HandleAsync(
        UpdateFinancialGatewayCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Spec is null)
        {
            return FinanceErrors.GatewayNameInvalid;
        }

        var targetId = FinancialGatewayId.From(command.GatewayId);
        var gateway = await dbContext.FinancialGateways.FirstOrDefaultAsync(
            g => g.Id == targetId,
            cancellationToken
        );
        if (gateway is null)
        {
            return FinanceErrors.GatewayNotFound;
        }

        var updated = gateway.Update(command.Spec);
        if (updated.IsFailure)
        {
            return Result.Failure<FinancialGatewayResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return FinancialGatewayMapper.ToResponse(gateway, registry);
    }
}
