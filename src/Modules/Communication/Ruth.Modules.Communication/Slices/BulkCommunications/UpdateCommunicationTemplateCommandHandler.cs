using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal sealed class UpdateCommunicationTemplateCommandHandler(CommunicationDbContext dbContext)
    : ICommandHandler<UpdateCommunicationTemplateCommand, CommunicationTemplateResponse>
{
    public async Task<Result<CommunicationTemplateResponse>> HandleAsync(
        UpdateCommunicationTemplateCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = CommunicationTemplateId.From(command.TemplateId);
        var template = await dbContext.CommunicationTemplates.FirstOrDefaultAsync(
            t => t.Id == id,
            cancellationToken
        );
        if (template is null)
        {
            return Result.Failure<CommunicationTemplateResponse>(
                BulkCommunicationErrors.TemplateNotFound
            );
        }

        var updated = template.Update(
            new CommunicationTemplateSpec(
                command.Name,
                template.Key,
                command.Channel,
                command.TopicKey,
                command.SubjectTemplate,
                command.Description,
                command.PreheaderText,
                command.FromName,
                command.FromEmail,
                command.ReplyToEmail,
                command.SenderSystemPhoneNumberId,
                command.RazorLayoutKey,
                command.SmsBodyTemplate,
                command.PushTitleTemplate,
                command.PushBodyTemplate,
                command.Sections,
                template.IsSystem,
                command.IsActive
            )
        );
        if (updated.IsFailure)
        {
            return Result.Failure<CommunicationTemplateResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return BulkCommunicationMapper.ToResponse(template);
    }
}
