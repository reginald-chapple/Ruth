using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.Modules.Crm.Infrastructure.Graph;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.People;

internal sealed class UpdatePersonCommandHandler(
    CrmDbContext dbContext,
    ICrmGraphService graphService,
    IClock clock
) : ICommandHandler<UpdatePersonCommand, PersonDetailResponse>
{
    public async Task<Result<PersonDetailResponse>> HandleAsync(
        UpdatePersonCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var person = await LoadPersonAsync(PersonId.From(command.PersonId), cancellationToken);
        if (person is null)
        {
            return CrmErrors.PersonNotFound;
        }

        var nowUtc = clock.UtcNow;
        var updated = person.Update(PersonMapper.ToInput(command, person.RecordType), nowUtc);
        if (updated.IsFailure)
        {
            return Result.Failure<PersonDetailResponse>(updated.Errors);
        }

        await MetaphoneIndexSynchronizer.EnsureIndexedAsync(dbContext, person, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        await graphService.UpsertPersonVertexAsync(
            person.Id.Value,
            person.FullName,
            cancellationToken
        );

        return Result.Success(
            PersonMapper.ToDetail(person, DateOnly.FromDateTime(nowUtc.UtcDateTime))
        );
    }

    private Task<Person?> LoadPersonAsync(PersonId personId, CancellationToken cancellationToken)
    {
        return dbContext
            .People.Include(static p => p.PhoneNumbers)
            .Include(static p => p.PreviousNames)
            .Include(static p => p.SearchKeys)
            .FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);
    }
}
