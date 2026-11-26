using Ruth.Modules.Lms.Contracts.Grading;
using Ruth.Modules.Lms.Domain.Grading;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Slices.Grading;

internal sealed class CreateLearningGradingSystemCommandHandler(LmsDbContext db)
    : ICommandHandler<CreateLearningGradingSystemCommand, LearningGradingSystemResponse>
{
    public async Task<Result<LearningGradingSystemResponse>> HandleAsync(
        CreateLearningGradingSystemCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var created = LearningGradingSystem.Create(
            command.Name,
            command.Scales,
            command.Description
        );
        if (created.IsFailure)
        {
            return Result.Failure<LearningGradingSystemResponse>(created.PrimaryError);
        }

        _ = db.GradingSystems.Add(created.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(LmsDtoMapper.ToResponse(created.Value));
    }
}
