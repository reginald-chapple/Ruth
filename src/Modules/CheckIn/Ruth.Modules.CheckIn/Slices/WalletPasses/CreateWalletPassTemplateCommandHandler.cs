using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Domain.WalletPasses;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.WalletPasses;

internal sealed class CreateWalletPassTemplateCommandHandler(CheckInDbContext dbContext)
    : ICommandHandler<CreateWalletPassTemplateCommand, WalletPassTemplateResponse>
{
    public async Task<Result<WalletPassTemplateResponse>> HandleAsync(
        CreateWalletPassTemplateCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var key = command.Key?.Trim() ?? string.Empty;
        var exists = await dbContext.WalletPassTemplates.AnyAsync(
            t => t.Key == key,
            cancellationToken
        );
        if (exists)
        {
            return Result.Failure<WalletPassTemplateResponse>(
                WalletPassErrors.TemplateDuplicateKey
            );
        }

        var created = WalletPassTemplate.Create(
            new WalletPassTemplateSpec(
                command.Name,
                key,
                command.PassTypeIdentifier,
                command.OrganizationName,
                command.Description,
                command.CardStyle,
                command.BackgroundColorHex,
                command.ForegroundColorHex,
                command.LabelColorHex,
                command.LogoText,
                command.DefaultValidityDays,
                command.IsActive
            )
        );
        if (created.IsFailure)
        {
            return Result.Failure<WalletPassTemplateResponse>(created.Errors);
        }

        _ = dbContext.WalletPassTemplates.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return WalletPassMapper.ToResponse(created.Value);
    }
}
