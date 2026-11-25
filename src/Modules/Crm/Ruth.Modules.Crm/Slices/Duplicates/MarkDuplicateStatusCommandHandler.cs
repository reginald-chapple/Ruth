using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Duplicates;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.Duplicates;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Slices.Duplicates;

internal sealed class MarkDuplicateStatusCommandHandler(CrmDbContext dbContext)
    : ICommandHandler<MarkDuplicateStatusCommand, PersonDuplicateResponse>
{
    public async Task<Result<PersonDuplicateResponse>> HandleAsync(
        MarkDuplicateStatusCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = PersonDuplicateId.From(command.DuplicateId);
        var duplicate = await dbContext.PersonDuplicates.FirstOrDefaultAsync(
            d => d.Id == id,
            cancellationToken
        );
        if (duplicate is null)
        {
            return CrmErrors.DuplicateNotFound;
        }

        duplicate.SetReviewStatus(
            command.IsConfirmedAsNotDuplicate,
            command.IgnoreUntilScoreChanges
        );
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        var p1 = await dbContext
            .People.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == duplicate.Person1Id, cancellationToken);
        var p2 = await dbContext
            .People.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == duplicate.Person2Id, cancellationToken);

        return Result.Success(
            new PersonDuplicateResponse(
                duplicate.Id.Value,
                duplicate.Person1Id.Value,
                p1?.FullName ?? duplicate.Person1Id.ToString(),
                p1?.Email,
                duplicate.Person2Id.Value,
                p2?.FullName ?? duplicate.Person2Id.ToString(),
                p2?.Email,
                duplicate.Score,
                duplicate.Capacity,
                duplicate.TotalCapacity,
                duplicate.ConfidenceScore,
                duplicate.ScoreDetail,
                duplicate.IsConfirmedAsNotDuplicate,
                duplicate.IgnoreUntilScoreChanges,
                duplicate.LastScannedOnUtc
            )
        );
    }
}
