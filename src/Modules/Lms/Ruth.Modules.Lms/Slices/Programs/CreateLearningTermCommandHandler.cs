using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Lms.Contracts.Programs;
using Ruth.Modules.Lms.Domain;
using Ruth.Modules.Lms.Domain.Programs;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Slices.Programs;

internal sealed class CreateLearningTermCommandHandler(LmsDbContext db)
    : ICommandHandler<CreateLearningTermCommand, LearningTermResponse>
{
    public async Task<Result<LearningTermResponse>> HandleAsync(
        CreateLearningTermCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var programId = LearningProgramId.From(command.LearningProgramId);
        var exists = await db
            .Programs.AsNoTracking()
            .AnyAsync(p => p.Id == programId, cancellationToken);
        if (!exists)
        {
            return Result.Failure<LearningTermResponse>(
                LmsErrors.NotFound("Program.NotFound", "LearningProgram was not found.")
            );
        }

        var created = LearningTerm.Create(
            programId,
            command.Name,
            command.PublicName,
            command.StartDate,
            command.EndDate,
            command.EnrollmentCloseDate
        );
        if (created.IsFailure)
        {
            return Result.Failure<LearningTermResponse>(created.PrimaryError);
        }

        _ = db.Terms.Add(created.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(LmsDtoMapper.ToResponse(created.Value));
    }
}
