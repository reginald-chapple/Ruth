using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Domain.WalletPasses;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.CheckIn.Slices.WalletPasses;

internal sealed class UpdateIssuedWalletPassDynamicFieldsCommandHandler(
    CheckInDbContext dbContext,
    WalletPassPushDispatchCollaborator pushCollaborator,
    IClock clock
) : ICommandHandler<UpdateIssuedWalletPassDynamicFieldsCommand, IssuedWalletPassResponse>
{
    public async Task<Result<IssuedWalletPassResponse>> HandleAsync(
        UpdateIssuedWalletPassDynamicFieldsCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = IssuedWalletPassId.From(command.IssuedPassId);
        var pass = await dbContext.IssuedWalletPasses.FirstOrDefaultAsync(
            p => p.Id == id,
            cancellationToken
        );
        if (pass is null)
        {
            return Result.Failure<IssuedWalletPassResponse>(WalletPassErrors.PassNotFound);
        }

        var updated = pass.UpdateDynamicFields(
            command.DynamicFields,
            command.ExpiresAtUtc,
            clock.UtcNow
        );
        if (updated.IsFailure)
        {
            return Result.Failure<IssuedWalletPassResponse>(updated.Errors);
        }

        var deviceCount = await pushCollaborator.DispatchPushUpdatesAndCountDevicesAsync(
            pass,
            cancellationToken
        );
        var templateName = await dbContext
            .WalletPassTemplates.AsNoTracking()
            .Where(t => t.Id == pass.TemplateId)
            .Select(static t => t.Name)
            .FirstOrDefaultAsync(cancellationToken)
            ?? "Wallet Pass";

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return WalletPassMapper.ToResponse(pass, templateName, deviceCount);
    }
}
