using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.Modules.Crm.Infrastructure.Graph;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.People;

internal sealed class CreatePersonCommandHandler(
    CrmDbContext dbContext,
    ICrmGraphService graphService,
    IClock clock
) : ICommandHandler<CreatePersonCommand, PersonDetailResponse>
{
    public async Task<Result<PersonDetailResponse>> HandleAsync(
        CreatePersonCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var nowUtc = clock.UtcNow;
        var created = Person.Create(PersonMapper.ToInput(command), nowUtc);
        if (created.IsFailure)
        {
            return Result.Failure<PersonDetailResponse>(created.Errors);
        }

        var person = created.Value;
        var attachResult = AttachOptionalData(person, command);
        if (attachResult.IsFailure)
        {
            return Result.Failure<PersonDetailResponse>(attachResult.Errors);
        }

        _ = dbContext.People.Add(person);
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

    private static Result AttachOptionalData(Person person, CreatePersonCommand command)
    {
        if (command.PhoneNumbers is not null)
        {
            foreach (var phone in command.PhoneNumbers)
            {
                var added = person.AddPhoneNumber(phone);
                if (added.IsFailure)
                {
                    return Result.Failure(added.Errors);
                }
            }
        }

        if (command.Attributes is not null)
        {
            person.ReplaceAttributes(command.Attributes);
        }

        return Result.Success();
    }
}
