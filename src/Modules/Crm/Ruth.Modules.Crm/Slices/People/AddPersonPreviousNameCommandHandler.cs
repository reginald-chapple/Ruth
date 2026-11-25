using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.People;

internal sealed class AddPersonPreviousNameCommandHandler(CrmDbContext dbContext, IClock clock)
    : ICommandHandler<AddPersonPreviousNameCommand, PersonDetailResponse>
{
    public async Task<Result<PersonDetailResponse>> HandleAsync(
        AddPersonPreviousNameCommand command,
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

        var added = person.AddPreviousName(command.LastName);
        if (added.IsFailure)
        {
            return Result.Failure<PersonDetailResponse>(added.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(
            PersonMapper.ToDetail(person, DateOnly.FromDateTime(clock.UtcNow.UtcDateTime))
        );
    }
}
