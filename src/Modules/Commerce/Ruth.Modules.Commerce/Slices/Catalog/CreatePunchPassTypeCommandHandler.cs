using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Domain.Catalog;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Catalog;

internal sealed class CreatePunchPassTypeCommandHandler(CommerceDbContext dbContext)
    : ICommandHandler<CreatePunchPassTypeCommand, PunchPassTypeResponse>
{
    public async Task<Result<PunchPassTypeResponse>> HandleAsync(
        CreatePunchPassTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var created = PunchPassType.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<PunchPassTypeResponse>(created.Errors);
        }

        var exists = await dbContext.PunchPassTypes.AnyAsync(
            p => p.Code == created.Value.Code,
            cancellationToken
        );
        if (exists)
        {
            return CommerceErrors.PunchPassTypeCodeConflict;
        }

        _ = dbContext.PunchPassTypes.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return CatalogMapper.MapPunchPassType(created.Value);
    }
}
