using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.Modules.Communication.Domain;
using Ruth.Modules.Communication.Domain.SystemCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Communication.Slices.SystemCommunications;

internal sealed class UpdateSystemCommunicationCommandHandler(
    CommunicationDbContext dbContext,
    IClock clock
) : ICommandHandler<UpdateSystemCommunicationCommand, SystemCommunicationResponse>
{
    public async Task<Result<SystemCommunicationResponse>> HandleAsync(
        UpdateSystemCommunicationCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = SystemCommunicationId.From(command.SystemCommunicationId);
        var entity = await dbContext.SystemCommunications.FirstOrDefaultAsync(
            c => c.Id == id,
            cancellationToken
        );
        if (entity is null)
        {
            return CommunicationDomainErrors.SystemCommunicationNotFound;
        }

        var normalizedKey = SystemCommunication.NormalizeKey(command.Key);
        if (normalizedKey.IsFailure)
        {
            return Result.Failure<SystemCommunicationResponse>(normalizedKey.Errors);
        }

        var duplicate = await dbContext.SystemCommunications.AnyAsync(
            c => c.Key == normalizedKey.Value && c.Id != id,
            cancellationToken
        );
        if (duplicate)
        {
            return CommunicationDomainErrors.SystemCommunicationKeyDuplicate;
        }

        var updated = entity.Update(
            new SystemCommunicationSpec(
                command.Title,
                normalizedKey.Value,
                command.Category,
                command.SubjectTemplate,
                command.HtmlBodyTemplate,
                command.FromName,
                command.FromEmail,
                command.RazorLayoutKey,
                command.SmsFromSystemPhoneNumberId,
                command.SmsMessageTemplate,
                command.PushTitleTemplate,
                command.PushMessageTemplate,
                command.PushOpenUrlTemplate,
                entity.IsSystem,
                command.IsActive
            )
        );
        if (updated.IsFailure)
        {
            return Result.Failure<SystemCommunicationResponse>(updated.Errors);
        }

        entity.SetModified(clock.UtcNow);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return SystemCommunicationMapper.ToResponse(entity);
    }
}
