using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.People;

internal sealed class UpsertPhoneNumberCommandHandler(CrmDbContext dbContext, IClock clock)
    : ICommandHandler<UpsertPhoneNumberCommand, PersonDetailResponse>
{
    public async Task<Result<PersonDetailResponse>> HandleAsync(
        UpsertPhoneNumberCommand command,
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

        Result outcome;
        if (command.PhoneNumberId.HasValue)
        {
            outcome = person.UpdatePhoneNumber(
                PhoneNumberId.From(command.PhoneNumberId.Value),
                command.Phone
            );
        }
        else
        {
            var added = person.AddPhoneNumber(command.Phone);
            outcome = added.IsFailure ? Result.Failure(added.Errors) : Result.Success();
        }

        if (outcome.IsFailure)
        {
            return Result.Failure<PersonDetailResponse>(outcome.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(
            PersonMapper.ToDetail(person, DateOnly.FromDateTime(clock.UtcNow.UtcDateTime))
        );
    }
}
