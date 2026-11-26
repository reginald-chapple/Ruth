using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.PaymentMethods;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.PaymentMethods;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.PaymentMethods;

internal sealed class DeactivateSavedPaymentMethodCommandHandler(FinanceDbContext dbContext)
    : ICommandHandler<DeactivateSavedPaymentMethodCommand, SavedPaymentMethodResponse>
{
    public async Task<Result<SavedPaymentMethodResponse>> HandleAsync(
        DeactivateSavedPaymentMethodCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var targetId = SavedPaymentMethodId.From(command.SavedPaymentMethodId);
        var method = await dbContext.SavedPaymentMethods.FirstOrDefaultAsync(
            m => m.Id == targetId,
            cancellationToken
        );
        if (method is null)
        {
            return FinanceErrors.PaymentMethodNotFound;
        }

        method.Deactivate();
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var gatewayName = await dbContext
            .FinancialGateways.AsNoTracking()
            .Where(g => g.Id == method.FinancialGatewayId)
            .Select(static g => g.Name)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "Gateway";

        return SavedPaymentMethodMapper.ToResponse(method, gatewayName);
    }
}
