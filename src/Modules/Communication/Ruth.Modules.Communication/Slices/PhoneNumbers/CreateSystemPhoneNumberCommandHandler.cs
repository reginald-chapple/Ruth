using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.PhoneNumbers;
using Ruth.Modules.Communication.Domain;
using Ruth.Modules.Communication.Domain.PhoneNumbers;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Communication.Slices.PhoneNumbers;

internal sealed class CreateSystemPhoneNumberCommandHandler(
    CommunicationDbContext dbContext,
    IClock clock
) : ICommandHandler<CreateSystemPhoneNumberCommand, SystemPhoneNumberResponse>
{
    public async Task<Result<SystemPhoneNumberResponse>> HandleAsync(
        CreateSystemPhoneNumberCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var created = SystemPhoneNumber.Create(
            command.Name,
            command.Number,
            command.Description,
            command.IsSmsEnabled,
            command.IsSmsForwardingEnabled,
            command.AssignedToPersonId,
            command.MobileApplicationSiteId,
            command.ProviderIdentifier,
            command.IsActive
        );
        if (created.IsFailure)
        {
            return Result.Failure<SystemPhoneNumberResponse>(created.Errors);
        }

        var duplicate = await dbContext.SystemPhoneNumbers.AnyAsync(
            x => x.Number == created.Value.Number,
            cancellationToken
        );
        if (duplicate)
        {
            return CommunicationDomainErrors.SystemPhoneNumberDuplicate;
        }

        created.Value.SetCreated(clock.UtcNow);
        _ = dbContext.SystemPhoneNumbers.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return SystemPhoneNumberMapper.ToResponse(created.Value);
    }
}
