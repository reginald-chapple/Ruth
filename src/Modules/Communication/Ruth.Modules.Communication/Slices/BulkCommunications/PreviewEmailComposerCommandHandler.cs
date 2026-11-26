using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal sealed class PreviewEmailComposerCommandHandler(BulkEmailComposerService composerService)
    : ICommandHandler<PreviewEmailComposerCommand, EmailComposerPreviewResponse>
{
    public async Task<Result<EmailComposerPreviewResponse>> HandleAsync(
        PreviewEmailComposerCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        if (
            string.IsNullOrWhiteSpace(command.SubjectTemplate)
            || command.SubjectTemplate.Trim().Length > 500
        )
        {
            return Result.Failure<EmailComposerPreviewResponse>(
                BulkCommunicationErrors.EmailSubjectRequired
            );
        }

        var sectionsResult = EmailSection.ValidateAndNormalize(command.Sections);
        return sectionsResult.IsFailure
                ? Result.Failure<EmailComposerPreviewResponse>(sectionsResult.Errors)
            : sectionsResult.Value.Count == 0
                ? Result.Failure<EmailComposerPreviewResponse>(
                    BulkCommunicationErrors.ChannelContentRequired
                )
            : await composerService.RenderEmailAsync(
                command.SubjectTemplate.Trim(),
                command.PreheaderText,
                command.RazorLayoutKey,
                sectionsResult.Value,
                htmlBodyOverride: null,
                command.IncludeUnsubscribeFooter,
                topicKey: "general-announcements",
                recipientName: "Jordan Taylor",
                normalizedDestination: "member@example.org",
                personId: null,
                command.SampleMergeFields,
                cancellationToken
            );
    }
}
