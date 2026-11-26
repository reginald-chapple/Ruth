using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Domain.WalletPasses;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.CheckIn.Slices.WalletPasses;

internal sealed class RevokeIssuedWalletPassCommandHandler(
    CheckInDbContext dbContext,
    WalletPassPushDispatchCollaborator pushCollaborator,
    IClock clock
) : ICommandHandler<RevokeIssuedWalletPassCommand, IssuedWalletPassResponse>
{
    public async Task<Result<IssuedWalletPassResponse>> HandleAsync(
        RevokeIssuedWalletPassCommand command,
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

        _ = pass.Revoke(command.Reason, clock.UtcNow);
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
