using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.People;

internal sealed class RemovePersonSearchKeyCommandHandler(CrmDbContext dbContext, IClock clock)
    : ICommandHandler<RemovePersonSearchKeyCommand, PersonDetailResponse>
{
    public async Task<Result<PersonDetailResponse>> HandleAsync(
        RemovePersonSearchKeyCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var personId = PersonId.From(command.PersonId);
        var person = await dbContext
            .People.Include(static p => p.PhoneNumbers)
            .Include(static p => p.PreviousNames)
            .Include(static p => p.SearchKeys)
            .FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);

        if (person is null)
        {
            return CrmErrors.PersonNotFound;
        }

        var removed = person.RemoveSearchKey(PersonSearchKeyId.From(command.SearchKeyId));
        if (removed.IsFailure)
        {
            return Result.Failure<PersonDetailResponse>(removed.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(
            PersonMapper.ToDetail(person, DateOnly.FromDateTime(clock.UtcNow.UtcDateTime))
        );
    }
}
