using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.PhoneNumbers;
using Ruth.Modules.Communication.Domain;
using Ruth.Modules.Communication.Domain.PhoneNumbers;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.PhoneNumbers;

internal sealed class DeleteSystemPhoneNumberCommandHandler(CommunicationDbContext dbContext)
    : ICommandHandler<DeleteSystemPhoneNumberCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteSystemPhoneNumberCommand command,
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

        var inUse =
            await dbContext.SystemCommunications.AnyAsync(
                c => c.SmsFromSystemPhoneNumberId == command.SystemPhoneNumberId,
                cancellationToken
            )
            || await dbContext.CommunicationTemplates.AnyAsync(
                t => t.SenderSystemPhoneNumberId == command.SystemPhoneNumberId,
                cancellationToken
            )
            || await dbContext.Communications.AnyAsync(
                c => c.SenderSystemPhoneNumberId == command.SystemPhoneNumberId,
                cancellationToken
            )
            || await dbContext.SmsPipelines.AnyAsync(
                p => p.SystemPhoneNumberId == command.SystemPhoneNumberId,
                cancellationToken
            );
        if (inUse)
        {
            return CommunicationDomainErrors.SystemPhoneNumberInUse;
        }

        _ = dbContext.SystemPhoneNumbers.Remove(entity);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
