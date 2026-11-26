using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.TwoWaySms;
using Ruth.Modules.Communication.Domain.PhoneNumbers;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.TwoWaySms;

internal sealed class MarkSmsConversationReadCommandHandler(CommunicationDbContext dbContext)
    : ICommandHandler<MarkSmsConversationReadCommand, int>
{
    public async Task<Result<int>> HandleAsync(
        MarkSmsConversationReadCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var normalized = SystemPhoneNumber.NormalizeE164(command.ParticipantPhoneNumber);
        if (normalized.IsFailure)
        {
            return Result.Failure<int>(normalized.Errors);
        }

        var unread = await dbContext
            .CommunicationResponses.Where(r =>
                r.SystemPhoneNumberId == command.SystemPhoneNumberId
                && r.ParticipantPhoneNumber == normalized.Value
                && !r.IsRead
            )
            .ToListAsync(cancellationToken);

        foreach (var item in unread)
        {
            item.MarkRead();
        }

        if (unread.Count > 0)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        return unread.Count;
    }
}
