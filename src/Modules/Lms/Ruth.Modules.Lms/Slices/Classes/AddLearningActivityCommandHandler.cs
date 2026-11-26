using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.Modules.Lms.Domain;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Slices.Classes;

internal sealed class AddLearningActivityCommandHandler(LmsDbContext db)
    : ICommandHandler<AddLearningActivityCommand, LearningActivityResponse>
{
    public async Task<Result<LearningActivityResponse>> HandleAsync(
        AddLearningActivityCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var classId = LearningClassId.From(command.LearningClassId);
        var cls = await db
            .Classes.Include(static c => c.Activities)
            .FirstOrDefaultAsync(c => c.Id == classId, cancellationToken);
        if (cls is null)
        {
            return Result.Failure<LearningActivityResponse>(
                LmsErrors.NotFound("Class.NotFound", "LearningClass was not found.")
            );
        }

        var actRes = cls.AddActivity(command);
        if (actRes.IsFailure)
        {
            return Result.Failure<LearningActivityResponse>(actRes.PrimaryError);
        }

        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(LmsDtoMapper.ToResponse(actRes.Value));
    }
}
