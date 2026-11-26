using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal sealed class CreateCommunicationCommandHandler(
    CommunicationDbContext dbContext,
    BulkRecipientResolver recipientResolver
) : ICommandHandler<CreateCommunicationCommand, CommunicationDetailResponse>
{
    public async Task<Result<CommunicationDetailResponse>> HandleAsync(
        CreateCommunicationCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var specResult = await HydrateFromTemplateIfNeededAsync(command, cancellationToken);
        if (specResult.IsFailure)
        {
            return Result.Failure<CommunicationDetailResponse>(specResult.Errors);
        }

        var draftResult = Domain.BulkCommunications.Communication.CreateDraft(specResult.Value);
        if (draftResult.IsFailure)
        {
            return Result.Failure<CommunicationDetailResponse>(draftResult.Errors);
        }

        var comm = draftResult.Value;
        var recipientsResult = await recipientResolver.BuildRecipientsAsync(
            comm.Id,
            comm.Channel,
            command.Recipients,
            command.TargetGroupId,
            cancellationToken
        );
        if (recipientsResult.IsFailure)
        {
            return Result.Failure<CommunicationDetailResponse>(recipientsResult.Errors);
        }

        _ = dbContext.Communications.Add(comm);
        if (recipientsResult.Value.Count > 0)
        {
            dbContext.CommunicationRecipients.AddRange(recipientsResult.Value);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return BulkCommunicationMapper.ToDetailResponse(comm, recipientsResult.Value);
    }

    private async Task<Result<CommunicationDraftSpec>> HydrateFromTemplateIfNeededAsync(
        CreateCommunicationCommand command,
        CancellationToken cancellationToken
    )
    {
        if (!command.TemplateId.HasValue || command.TemplateId.Value == Guid.Empty)
        {
            return BuildDirectSpec(command);
        }

        var templateId = CommunicationTemplateId.From(command.TemplateId.Value);
        var template = await dbContext
            .CommunicationTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        return template is null
            ? Result.Failure<CommunicationDraftSpec>(BulkCommunicationErrors.TemplateNotFound)
            : MergeTemplateDefaults(command, template);
    }

    private static CommunicationDraftSpec BuildDirectSpec(CreateCommunicationCommand command)
    {
        var topic = string.IsNullOrWhiteSpace(command.TopicKey)
            ? "general-announcements"
            : command.TopicKey;
        return new CommunicationDraftSpec(
            command.Title,
            command.Channel,
            topic,
            command.IsBulk,
            command.TemplateId,
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
            command.CreatedByPersonId
        );
    }

    private static CommunicationDraftSpec MergeTemplateDefaults(
        CreateCommunicationCommand command,
        CommunicationTemplate template
    )
    {
        var sections = command.Sections is { Count: > 0 }
            ? command.Sections
            : [.. template.Sections.Select(static s => s.ToDto())];
        var topic = string.IsNullOrWhiteSpace(command.TopicKey)
            ? template.TopicKey
            : command.TopicKey;

        return new CommunicationDraftSpec(
            command.Title,
            command.Channel,
            topic,
            command.IsBulk,
            command.TemplateId,
            Pick(command.Subject, template.SubjectTemplate),
            Pick(command.PreheaderText, template.PreheaderText),
            Pick(command.FromName, template.FromName),
            Pick(command.FromEmail, template.FromEmail),
            Pick(command.ReplyToEmail, template.ReplyToEmail),
            command.SenderSystemPhoneNumberId ?? template.SenderSystemPhoneNumberId,
            Pick(command.RazorLayoutKey, template.RazorLayoutKey),
            command.HtmlBodyOverride,
            Pick(command.SmsBody, template.SmsBodyTemplate),
            Pick(command.PushTitle, template.PushTitleTemplate),
            Pick(command.PushBody, template.PushBodyTemplate),
            command.PushOpenUrl,
            sections,
            command.ScheduledSendAtUtc,
            command.CreatedByPersonId
        );
    }

    private static string? Pick(string? primary, string? fallback)
    {
        return string.IsNullOrWhiteSpace(primary) ? fallback : primary;
    }
}
