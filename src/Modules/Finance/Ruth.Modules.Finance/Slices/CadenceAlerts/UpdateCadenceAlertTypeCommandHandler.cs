using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.CadenceAlerts;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.CadenceAlerts;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.CadenceAlerts;

internal sealed class UpdateCadenceAlertTypeCommandHandler(FinanceDbContext dbContext)
    : ICommandHandler<UpdateCadenceAlertTypeCommand, TransactionCadenceAlertTypeResponse>
{
    public async Task<Result<TransactionCadenceAlertTypeResponse>> HandleAsync(
        UpdateCadenceAlertTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Spec is null)
        {
            return FinanceErrors.CadenceAlertTypeInvalid;
        }

        var id = TransactionCadenceAlertTypeId.From(command.AlertTypeId);
        var entity = await dbContext.CadenceAlertTypes.FirstOrDefaultAsync(
            t => t.Id == id,
            cancellationToken
        );
        if (entity is null)
        {
            return FinanceErrors.CadenceAlertNotFound;
        }

        var updated = entity.Update(command.Spec);
        if (updated.IsFailure)
        {
            return Result.Failure<TransactionCadenceAlertTypeResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return CadenceAlertMapper.ToTypeResponse(entity);
    }
}
