using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.Labels;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.Labels;

internal sealed class UpdateCheckInLabelCommandHandler(CheckInDbContext dbContext)
    : ICommandHandler<UpdateCheckInLabelCommand, CheckInLabelResponse>
{
    public async Task<Result<CheckInLabelResponse>> HandleAsync(
        UpdateCheckInLabelCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = CheckInLabelId.From(command.LabelId);
        var label = await dbContext.CheckInLabels.FirstOrDefaultAsync(
            l => l.Id == id,
            cancellationToken
        );
        if (label is null)
        {
            return Result.Failure<CheckInLabelResponse>(CheckInErrors.LabelNotFound);
        }

        var candidateKey = command.Key?.Trim().ToLowerInvariant() ?? string.Empty;
        var duplicate = await dbContext
            .CheckInLabels.AsNoTracking()
            .AnyAsync(l => l.Id != id && l.Key == candidateKey, cancellationToken);
        if (duplicate)
        {
            return Result.Failure<CheckInLabelResponse>(CheckInErrors.LabelDuplicateKey);
        }

        var updated = label.Update(CheckInLabelMapper.ToSpec(command, label.IsSystem));
        if (updated.IsFailure)
        {
            return Result.Failure<CheckInLabelResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(CheckInLabelMapper.ToResponse(label));
    }
}
