using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.Modules.Communication.Domain;
using Ruth.Modules.Communication.Domain.SystemCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Communication.Slices.SystemCommunications;

internal sealed class CreateSystemCommunicationCommandHandler(
    CommunicationDbContext dbContext,
    IClock clock
) : ICommandHandler<CreateSystemCommunicationCommand, SystemCommunicationResponse>
{
    public async Task<Result<SystemCommunicationResponse>> HandleAsync(
        CreateSystemCommunicationCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var created = SystemCommunication.Create(
            new SystemCommunicationSpec(
                command.Title,
                command.Key,
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
                IsSystem: false,
                command.IsActive
            )
        );
        if (created.IsFailure)
        {
            return Result.Failure<SystemCommunicationResponse>(created.Errors);
        }

        var duplicate = await dbContext.SystemCommunications.AnyAsync(
            c => c.Key == created.Value.Key,
            cancellationToken
        );
        if (duplicate)
        {
            return CommunicationDomainErrors.SystemCommunicationKeyDuplicate;
        }

        created.Value.SetCreated(clock.UtcNow);
        _ = dbContext.SystemCommunications.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return SystemCommunicationMapper.ToResponse(created.Value);
    }
}
