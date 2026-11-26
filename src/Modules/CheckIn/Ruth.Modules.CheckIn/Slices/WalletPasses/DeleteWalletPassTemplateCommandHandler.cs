using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Domain.WalletPasses;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.WalletPasses;

internal sealed class DeleteWalletPassTemplateCommandHandler(CheckInDbContext dbContext)
    : ICommandHandler<DeleteWalletPassTemplateCommand>
{
    public async Task<Result> HandleAsync(
        DeleteWalletPassTemplateCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = WalletPassTemplateId.From(command.TemplateId);
        var template = await dbContext.WalletPassTemplates.FirstOrDefaultAsync(
            t => t.Id == id,
            cancellationToken
        );
        if (template is null)
        {
            return WalletPassErrors.TemplateNotFound;
        }

        var canDelete = template.EnsureCanDelete();
        if (canDelete.IsFailure)
        {
            return canDelete;
        }

        var hasIssued = await dbContext.IssuedWalletPasses.AnyAsync(
            p => p.TemplateId == id,
            cancellationToken
        );
        if (hasIssued)
        {
            return WalletPassErrors.TemplateHasIssuedPasses;
        }

        _ = dbContext.WalletPassTemplates.Remove(template);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
