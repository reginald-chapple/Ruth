using Ruth.Modules.Lms.Contracts.Programs;
using Ruth.Modules.Lms.Domain;
using Ruth.Modules.Lms.Domain.Programs;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Slices.Programs;

internal sealed class CreateLearningProgramCommandHandler(LmsDbContext db)
    : ICommandHandler<CreateLearningProgramCommand, LearningProgramResponse>
{
    public async Task<Result<LearningProgramResponse>> HandleAsync(
        CreateLearningProgramCommand command,
        CancellationToken cancellationToken = default
    )
    {
        LearningGradingSystemId? gradingId =
            command.DefaultGradingSystemId is { } gid && gid != Guid.Empty
                ? LearningGradingSystemId.From(gid)
                : null;

        var created = LearningProgram.Create(
            command.Name,
            command.PublicName,
            command.ConfigurationMode,
            command.Summary,
            command.Description,
            command.CategoryId,
            gradingId,
            command.HighlightColor,
            command.IconCssClass,
            command.IsPublic,
            command.IsCompletionStatusTracked
        );
        if (created.IsFailure)
        {
            return Result.Failure<LearningProgramResponse>(created.PrimaryError);
        }

        _ = db.Programs.Add(created.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(LmsDtoMapper.ToResponse(created.Value));
    }
}
