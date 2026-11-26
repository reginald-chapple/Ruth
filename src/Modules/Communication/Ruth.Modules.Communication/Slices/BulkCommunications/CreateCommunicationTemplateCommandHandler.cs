using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal sealed class CreateCommunicationTemplateCommandHandler(CommunicationDbContext dbContext)
    : ICommandHandler<CreateCommunicationTemplateCommand, CommunicationTemplateResponse>
{
    public async Task<Result<CommunicationTemplateResponse>> HandleAsync(
        CreateCommunicationTemplateCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var normalizedKey = command.Key?.Trim() ?? string.Empty;
        var exists = await dbContext.CommunicationTemplates.AnyAsync(
            t => t.Key == normalizedKey,
            cancellationToken
        );
        if (exists)
        {
            return Result.Failure<CommunicationTemplateResponse>(
                BulkCommunicationErrors.TemplateKeyDuplicate
            );
        }

        var created = CommunicationTemplate.Create(
            new CommunicationTemplateSpec(
                command.Name,
                normalizedKey,
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
                IsSystem: false,
                IsActive: command.IsActive
            )
        );
        if (created.IsFailure)
        {
            return Result.Failure<CommunicationTemplateResponse>(created.Errors);
        }

        _ = dbContext.CommunicationTemplates.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return BulkCommunicationMapper.ToResponse(created.Value);
    }
}
