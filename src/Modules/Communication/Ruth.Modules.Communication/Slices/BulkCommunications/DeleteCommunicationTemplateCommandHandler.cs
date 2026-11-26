using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal sealed class DeleteCommunicationTemplateCommandHandler(CommunicationDbContext dbContext)
    : ICommandHandler<DeleteCommunicationTemplateCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteCommunicationTemplateCommand command,
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
            return Result.Failure<bool>(BulkCommunicationErrors.TemplateNotFound);
        }

        var check = template.EnsureCanDelete();
        if (check.IsFailure)
        {
            return Result.Failure<bool>(check.Errors);
        }

        _ = dbContext.CommunicationTemplates.Remove(template);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
