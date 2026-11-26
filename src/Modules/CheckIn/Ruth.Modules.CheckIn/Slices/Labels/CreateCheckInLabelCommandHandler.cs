using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.Labels;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Domain.Labels;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.Labels;

internal sealed class CreateCheckInLabelCommandHandler(CheckInDbContext dbContext)
    : ICommandHandler<CreateCheckInLabelCommand, CheckInLabelResponse>
{
    public async Task<Result<CheckInLabelResponse>> HandleAsync(
        CreateCheckInLabelCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var created = CheckInLabel.Create(CheckInLabelMapper.ToSpec(command));
        if (created.IsFailure)
        {
            return Result.Failure<CheckInLabelResponse>(created.Errors);
        }

        var exists = await dbContext
            .CheckInLabels.AsNoTracking()
            .AnyAsync(l => l.Key == created.Value.Key, cancellationToken);
        if (exists)
        {
            return Result.Failure<CheckInLabelResponse>(CheckInErrors.LabelDuplicateKey);
        }

        _ = dbContext.CheckInLabels.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(CheckInLabelMapper.ToResponse(created.Value));
    }
}
