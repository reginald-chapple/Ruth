using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal sealed class UpdateCommunicationDraftCommandHandler(
    CommunicationDbContext dbContext,
    BulkRecipientResolver recipientResolver
) : ICommandHandler<UpdateCommunicationDraftCommand, CommunicationDetailResponse>
{
    public async Task<Result<CommunicationDetailResponse>> HandleAsync(
        UpdateCommunicationDraftCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = CommunicationId.From(command.CommunicationId);
        var comm = await dbContext.Communications.FirstOrDefaultAsync(
            c => c.Id == id,
            cancellationToken
        );
        if (comm is null)
        {
            return Result.Failure<CommunicationDetailResponse>(
                BulkCommunicationErrors.CommunicationNotFound
            );
        }

        var updated = comm.UpdateDraft(
            new CommunicationDraftSpec(
                command.Title,
                comm.Channel,
                command.TopicKey,
                command.IsBulk,
                comm.TemplateId,
                command.Subject,
                command.PreheaderText,
                command.FromName,
                command.FromEmail,
                command.ReplyToEmail,
                command.SenderSystemPhoneNumberId,
                command.RazorLayoutKey,
                command.HtmlBodyOverride,
                command.SmsBody,
                command.PushTitle,
                command.PushBody,
                command.PushOpenUrl,
                command.Sections,
                command.ScheduledSendAtUtc,
                comm.CreatedByPersonId
            )
        );
        if (updated.IsFailure)
        {
            return Result.Failure<CommunicationDetailResponse>(updated.Errors);
        }

        var recipientsResult = await UpdateRecipientsIfProvidedAsync(
            comm,
            command.Recipients,
            cancellationToken
        );
        if (recipientsResult.IsFailure)
        {
            return Result.Failure<CommunicationDetailResponse>(recipientsResult.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return BulkCommunicationMapper.ToDetailResponse(comm, recipientsResult.Value);
    }

    private async Task<Result<List<CommunicationRecipient>>> UpdateRecipientsIfProvidedAsync(
        Domain.BulkCommunications.Communication comm,
        IReadOnlyList<CommunicationRecipientInputDto>? replacementInputs,
        CancellationToken cancellationToken
    )
    {
        var existing = await dbContext
            .CommunicationRecipients.Where(r => r.CommunicationId == comm.Id)
            .ToListAsync(cancellationToken);
        if (replacementInputs is null)
        {
            return existing;
        }

        var resolved = await recipientResolver.BuildRecipientsAsync(
            comm.Id,
            comm.Channel,
            replacementInputs,
            targetGroupId: null,
            cancellationToken
        );
        if (resolved.IsFailure)
        {
            return resolved;
        }

        if (existing.Count > 0)
        {
            dbContext.CommunicationRecipients.RemoveRange(existing);
        }

        if (resolved.Value.Count > 0)
        {
            dbContext.CommunicationRecipients.AddRange(resolved.Value);
        }

        return resolved;
    }
}
