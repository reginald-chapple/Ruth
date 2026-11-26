using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.PhoneNumbers;
using Ruth.Modules.Communication.Domain;
using Ruth.Modules.Communication.Domain.PhoneNumbers;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Communication.Slices.PhoneNumbers;

internal sealed class UpdateSystemPhoneNumberCommandHandler(
    CommunicationDbContext dbContext,
    IClock clock
) : ICommandHandler<UpdateSystemPhoneNumberCommand, SystemPhoneNumberResponse>
{
    public async Task<Result<SystemPhoneNumberResponse>> HandleAsync(
        UpdateSystemPhoneNumberCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = SystemPhoneNumberId.From(command.SystemPhoneNumberId);
        var entity = await dbContext.SystemPhoneNumbers.FirstOrDefaultAsync(
            x => x.Id == id,
            cancellationToken
        );
        if (entity is null)
        {
            return CommunicationDomainErrors.SystemPhoneNumberNotFound;
        }

        var normalized = SystemPhoneNumber.NormalizeE164(command.Number);
        if (normalized.IsFailure)
        {
            return Result.Failure<SystemPhoneNumberResponse>(normalized.Errors);
        }

        var duplicate = await dbContext.SystemPhoneNumbers.AnyAsync(
            x => x.Number == normalized.Value && x.Id != id,
            cancellationToken
        );
        if (duplicate)
        {
            return CommunicationDomainErrors.SystemPhoneNumberDuplicate;
        }

        var updated = entity.Update(
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
        if (updated.IsFailure)
        {
            return Result.Failure<SystemPhoneNumberResponse>(updated.Errors);
        }

        entity.SetModified(clock.UtcNow);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return SystemPhoneNumberMapper.ToResponse(entity);
    }
}
