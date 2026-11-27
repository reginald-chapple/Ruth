using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Communication.Slices.BulkCommunications;

internal sealed class ApproveCommunicationCommandHandler(
    CommunicationDbContext dbContext,
    ITenantContext tenantContext
) : ICommandHandler<ApproveCommunicationCommand, CommunicationDetailResponse>
{
    public async Task<Result<CommunicationDetailResponse>> HandleAsync(
        ApproveCommunicationCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!tenantContext.IsLive)
        {
            return Result.Failure<CommunicationDetailResponse>(
                BulkCommunicationErrors.TenantNotLive
            );
        }

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

        var recipients = await dbContext
            .CommunicationRecipients.AsNoTracking()
            .Where(r => r.CommunicationId == id)
            .ToListAsync(cancellationToken);

        var outcome = comm.Approve(
            command.ReviewerPersonId,
            command.ApprovalNote,
            recipients.Count
        );
        if (outcome.IsFailure)
        {
            return Result.Failure<CommunicationDetailResponse>(outcome.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return BulkCommunicationMapper.ToDetailResponse(comm, recipients);
    }
}
