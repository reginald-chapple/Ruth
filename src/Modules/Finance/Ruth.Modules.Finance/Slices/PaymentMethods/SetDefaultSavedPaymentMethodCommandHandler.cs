using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.PaymentMethods;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.PaymentMethods;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.PaymentMethods;

internal sealed class SetDefaultSavedPaymentMethodCommandHandler(FinanceDbContext dbContext)
    : ICommandHandler<SetDefaultSavedPaymentMethodCommand, SavedPaymentMethodResponse>
{
    public async Task<Result<SavedPaymentMethodResponse>> HandleAsync(
        SetDefaultSavedPaymentMethodCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var targetId = SavedPaymentMethodId.From(command.SavedPaymentMethodId);
        var method = await dbContext.SavedPaymentMethods.FirstOrDefaultAsync(
            m => m.Id == targetId && m.IsActive,
            cancellationToken
        );
        if (method is null)
        {
            return FinanceErrors.PaymentMethodNotFound;
        }

        var siblings = await dbContext
            .SavedPaymentMethods.Where(m => m.PersonId == method.PersonId && m.Id != targetId)
            .ToListAsync(cancellationToken);
        foreach (var sibling in siblings)
        {
            sibling.SetDefault(isDefault: false);
        }

        method.SetDefault(isDefault: true);
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
