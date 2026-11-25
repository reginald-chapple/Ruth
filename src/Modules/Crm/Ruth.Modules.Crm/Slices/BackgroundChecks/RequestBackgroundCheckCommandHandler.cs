using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.BackgroundChecks;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.BackgroundChecks;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.Modules.Crm.Infrastructure.BackgroundChecks;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Crm.Slices.BackgroundChecks;

internal sealed class RequestBackgroundCheckCommandHandler(
    CrmDbContext dbContext,
    IBackgroundCheckProviderResolver providerResolver,
    IClock clock
) : ICommandHandler<RequestBackgroundCheckCommand, BackgroundCheckResponse>
{
    public async Task<Result<BackgroundCheckResponse>> HandleAsync(
        RequestBackgroundCheckCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var provider = providerResolver.Resolve(command.ProviderKey);
        if (provider is null)
        {
            return CrmErrors.BackgroundCheckProviderNotFound;
        }

        var personId = PersonId.From(command.PersonId);
        var person = await dbContext
            .People.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == personId, cancellationToken);
        if (person is null)
        {
            return CrmErrors.PersonNotFound;
        }

        var checkId = BackgroundCheckId.Create();
        var submission = await provider.SubmitRequestAsync(
            new BackgroundCheckSubmissionRequest(
                checkId.Value,
                personId.Value,
                person.FirstName,
                person.MiddleName,
                person.LastName,
                person.Email,
                command.PackageName
            ),
            cancellationToken
        );
        if (submission.IsFailure)
        {
            return Result.Failure<BackgroundCheckResponse>(submission.Errors);
        }

        var check = BackgroundCheck.Create(
            checkId,
            personId,
            provider.ProviderKey,
            command.PackageName,
            submission.Value.ProviderRequestId,
            submission.Value.InitialStatus,
            submission.Value.RawProviderStatus,
            clock.UtcNow,
            command.RequestedByUserId
        );

        _ = dbContext.BackgroundChecks.Add(check);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(ToResponse(check));
    }

    internal static BackgroundCheckResponse ToResponse(BackgroundCheck b)
    {
        return new BackgroundCheckResponse(
            b.Id.Value,
            b.PersonId.Value,
            b.ProviderKey,
            b.PackageName,
            b.RequestId,
            b.ResponseId,
            b.ResponseDocumentBinaryFileId,
            b.RequestedOnUtc,
            b.RespondedOnUtc,
            b.Status,
            b.RawProviderStatus,
            b.RecordFound,
            b.Recommendation,
            b.CreatedOnUtc
        );
    }
}
