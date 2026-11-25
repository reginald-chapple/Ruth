using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.BackgroundChecks;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Slices.BackgroundChecks;

internal sealed class GetPersonBackgroundChecksQueryHandler(CrmDbContext dbContext)
    : IQueryHandler<GetPersonBackgroundChecksQuery, IReadOnlyList<BackgroundCheckResponse>>
{
    public async Task<Result<IReadOnlyList<BackgroundCheckResponse>>> HandleAsync(
        GetPersonBackgroundChecksQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var personId = PersonId.From(query.PersonId);
        var checks = await dbContext
            .BackgroundChecks.AsNoTracking()
            .Where(b => b.PersonId == personId)
            .OrderByDescending(static b => b.RequestedOnUtc)
            .ToListAsync(cancellationToken);

        IReadOnlyList<BackgroundCheckResponse> result =
        [
            .. checks.Select(RequestBackgroundCheckCommandHandler.ToResponse),
        ];
        return Result.Success(result);
    }
}
