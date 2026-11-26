using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.Modules.Communication.Domain;
using Ruth.Modules.Communication.Domain.SystemCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.SystemCommunications;

internal sealed class DeleteSystemCommunicationCommandHandler(CommunicationDbContext dbContext)
    : ICommandHandler<DeleteSystemCommunicationCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteSystemCommunicationCommand command,
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

        var canDelete = entity.EnsureCanDelete();
        if (canDelete.IsFailure)
        {
            return Result.Failure<bool>(canDelete.Errors);
        }

        _ = dbContext.SystemCommunications.Remove(entity);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
