using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Finance.Contracts.CadenceAlerts;
using Ruth.Modules.Finance.Domain;
using Ruth.Modules.Finance.Domain.CadenceAlerts;
using Ruth.Modules.Finance.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Slices.CadenceAlerts;

internal sealed class UpdateCadenceAlertStatusCommandHandler(FinanceDbContext dbContext)
    : ICommandHandler<UpdateCadenceAlertStatusCommand, TransactionCadenceAlertResponse>
{
    public async Task<Result<TransactionCadenceAlertResponse>> HandleAsync(
        UpdateCadenceAlertStatusCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var alertId = TransactionCadenceAlertId.From(command.AlertId);
        var alert = await dbContext.CadenceAlerts.FirstOrDefaultAsync(
            a => a.Id == alertId,
            cancellationToken
        );
        if (alert is null)
        {
            return FinanceErrors.CadenceAlertNotFound;
        }

        alert.TransitionStatus(command.NewStatus, command.Notes);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var typeName = await dbContext
            .CadenceAlertTypes.AsNoTracking()
            .Where(t => t.Id == alert.AlertTypeId)
            .Select(static t => t.Name)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "Cadence Rule";

        return CadenceAlertMapper.ToAlertResponse(alert, typeName);
    }
}
