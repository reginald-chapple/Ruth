using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Catalog;

internal sealed class UpdatePunchPassTypeCommandHandler(CommerceDbContext dbContext)
    : ICommandHandler<UpdatePunchPassTypeCommand, PunchPassTypeResponse>
{
    public async Task<Result<PunchPassTypeResponse>> HandleAsync(
        UpdatePunchPassTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = PunchPassTypeId.From(command.PunchPassTypeId);
        var entity = await dbContext.PunchPassTypes.FirstOrDefaultAsync(
            p => p.Id == id,
            cancellationToken
        );
        if (entity is null)
        {
            return CommerceErrors.PunchPassTypeNotFound;
        }

        var normalizedCode = command.Spec.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        var duplicate = await dbContext.PunchPassTypes.AnyAsync(
            p => p.Id != id && p.Code == normalizedCode,
            cancellationToken
        );
        if (duplicate)
        {
            return CommerceErrors.PunchPassTypeCodeConflict;
        }

        var updated = entity.Update(command.Spec);
        if (updated.IsFailure)
        {
            return Result.Failure<PunchPassTypeResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return CatalogMapper.MapPunchPassType(entity);
    }
}
