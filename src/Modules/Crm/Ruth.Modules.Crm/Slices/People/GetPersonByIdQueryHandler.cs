using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.People;

internal sealed class GetPersonByIdQueryHandler(CrmDbContext dbContext, IClock clock)
    : IQueryHandler<GetPersonByIdQuery, PersonDetailResponse>
{
    public async Task<Result<PersonDetailResponse>> HandleAsync(
        GetPersonByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var targetId = PersonId.From(query.PersonId);
        if (query.FollowRedirects)
        {
            targetId = await ResolveRedirectAsync(dbContext, targetId, cancellationToken);
        }

        var person = await dbContext
            .People.AsNoTracking()
            .AsSplitQuery()
            .Include(static p => p.PhoneNumbers)
            .Include(static p => p.PreviousNames)
            .Include(static p => p.SearchKeys)
            .FirstOrDefaultAsync(p => p.Id == targetId, cancellationToken);

        return person is null
            ? CrmErrors.PersonNotFound
            : Result.Success(
                PersonMapper.ToDetail(person, DateOnly.FromDateTime(clock.UtcNow.UtcDateTime))
            );
    }

    internal static async Task<PersonId> ResolveRedirectAsync(
        CrmDbContext dbContext,
        PersonId requestedId,
        CancellationToken cancellationToken
    )
    {
        var current = requestedId;
        var visited = new HashSet<PersonId> { current };

        for (var hop = 0; hop < 10; hop++)
        {
            var redirect = await dbContext
                .PersonRedirects.AsNoTracking()
                .Where(r =>
                    r.SourcePersonId == current
                    || (
                        r.ExternalRecordGuid.HasValue && r.ExternalRecordGuid.Value == current.Value
                    )
                )
                .Select(static r => r.TargetPersonId)
                .FirstOrDefaultAsync(cancellationToken);

            if (redirect == PersonId.Empty || !visited.Add(redirect))
            {
                break;
            }

            current = redirect;
        }

        return current;
    }
}
