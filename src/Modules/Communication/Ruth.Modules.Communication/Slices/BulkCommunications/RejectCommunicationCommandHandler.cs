using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal sealed class RejectCommunicationCommandHandler(CommunicationDbContext dbContext)
    : ICommandHandler<RejectCommunicationCommand, CommunicationDetailResponse>
{
    public async Task<Result<CommunicationDetailResponse>> HandleAsync(
        RejectCommunicationCommand command,
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

        var outcome = comm.Reject(command.ReviewerPersonId, command.Reason);
        if (outcome.IsFailure)
        {
            return Result.Failure<CommunicationDetailResponse>(outcome.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        var recipients = await dbContext
            .CommunicationRecipients.AsNoTracking()
            .Where(r => r.CommunicationId == id)
            .ToListAsync(cancellationToken);
        return BulkCommunicationMapper.ToDetailResponse(comm, recipients);
    }
}
