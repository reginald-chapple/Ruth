using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.Labels;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.Labels;

internal sealed class GetCheckInLabelByIdQueryHandler(CheckInDbContext dbContext)
    : IQueryHandler<GetCheckInLabelByIdQuery, CheckInLabelResponse>
{
    public async Task<Result<CheckInLabelResponse>> HandleAsync(
        GetCheckInLabelByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var id = CheckInLabelId.From(query.LabelId);
        var label = await dbContext
            .CheckInLabels.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        return label is null
            ? Result.Failure<CheckInLabelResponse>(CheckInErrors.LabelNotFound)
            : Result.Success(CheckInLabelMapper.ToResponse(label));
    }
}
