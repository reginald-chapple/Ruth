using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.Labels;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.Labels;

internal sealed class DeleteCheckInLabelCommandHandler(CheckInDbContext dbContext)
    : ICommandHandler<DeleteCheckInLabelCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteCheckInLabelCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = CheckInLabelId.From(command.LabelId);
        var label = await dbContext.CheckInLabels.FirstOrDefaultAsync(
            l => l.Id == id,
            cancellationToken
        );
        if (label is null)
        {
            return Result.Failure<bool>(CheckInErrors.LabelNotFound);
        }

        var deletable = label.EnsureDeletable();
        if (deletable.IsFailure)
        {
            return Result.Failure<bool>(deletable.Errors);
        }

        _ = dbContext.CheckInLabels.Remove(label);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(value: true);
    }
}
