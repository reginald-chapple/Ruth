using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Campuses;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Campuses;

internal sealed class DeleteCampusCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<DeleteCampusCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteCampusCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = CampusId.From(command.CampusId);
        var campus = await dbContext.Campuses.FirstOrDefaultAsync(
            c => c.Id == id,
            cancellationToken
        );
        if (campus is null)
        {
            return CoreLocationErrors.CampusNotFound;
        }

        if (campus.IsSystem)
        {
            return CoreErrors.SystemItemProtected;
        }

        var totalCampuses = await dbContext.Campuses.CountAsync(cancellationToken);
        if (totalCampuses <= 1)
        {
            return CoreLocationErrors.CampusLastCannotDelete;
        }

        _ = dbContext.Campuses.Remove(campus);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(value: true);
    }
}
