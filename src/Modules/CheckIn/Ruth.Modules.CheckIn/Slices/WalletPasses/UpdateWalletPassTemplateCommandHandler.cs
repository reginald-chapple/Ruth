using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Domain.WalletPasses;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.WalletPasses;

internal sealed class UpdateWalletPassTemplateCommandHandler(CheckInDbContext dbContext)
    : ICommandHandler<UpdateWalletPassTemplateCommand, WalletPassTemplateResponse>
{
    public async Task<Result<WalletPassTemplateResponse>> HandleAsync(
        UpdateWalletPassTemplateCommand command,
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
            return Result.Failure<WalletPassTemplateResponse>(WalletPassErrors.TemplateNotFound);
        }

        var updated = template.Update(
            new WalletPassTemplateSpec(
                command.Name,
                template.Key,
                command.PassTypeIdentifier,
                command.OrganizationName,
                command.Description,
                command.CardStyle,
                command.BackgroundColorHex,
                command.ForegroundColorHex,
                command.LabelColorHex,
                command.LogoText,
                command.DefaultValidityDays,
                command.IsActive,
                template.IsSystem,
                template.SystemGuid
            )
        );
        if (updated.IsFailure)
        {
            return Result.Failure<WalletPassTemplateResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return WalletPassMapper.ToResponse(template);
    }
}
