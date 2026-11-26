using Ruth.Modules.Finance.Contracts.CadenceAlerts;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.CadenceAlerts;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.CadenceAlerts;

internal sealed class CreateCadenceAlertTypeCommandHandler(FinanceDbContext dbContext)
    : ICommandHandler<CreateCadenceAlertTypeCommand, TransactionCadenceAlertTypeResponse>
{
    public async Task<Result<TransactionCadenceAlertTypeResponse>> HandleAsync(
        CreateCadenceAlertTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Spec is null)
        {
            return FinanceErrors.CadenceAlertTypeInvalid;
        }

        var created = TransactionCadenceAlertType.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<TransactionCadenceAlertTypeResponse>(created.Errors);
        }

        _ = dbContext.CadenceAlertTypes.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return CadenceAlertMapper.ToTypeResponse(created.Value);
    }
}
