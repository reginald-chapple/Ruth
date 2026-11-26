using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.Modules.Lms.Domain;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Lms.Slices.Classes;

internal sealed class AddClassContentPageCommandHandler(LmsDbContext db)
    : ICommandHandler<AddClassContentPageCommand, LearningClassContentPageResponse>
{
    public async Task<Result<LearningClassContentPageResponse>> HandleAsync(
        AddClassContentPageCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var classId = LearningClassId.From(command.LearningClassId);
        var cls = await db
            .Classes.Include(static c => c.ContentPages)
            .FirstOrDefaultAsync(c => c.Id == classId, cancellationToken);
        if (cls is null)
        {
            return Result.Failure<LearningClassContentPageResponse>(
                LmsErrors.NotFound("Class.NotFound", "LearningClass was not found.")
            );
        }

        var pageRes = cls.AddContentPage(
            command.Title,
            command.ContentHtml,
            command.Order,
            command.StartDateTimeUtc
        );
        if (pageRes.IsFailure)
        {
            return Result.Failure<LearningClassContentPageResponse>(pageRes.PrimaryError);
        }

        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(LmsDtoMapper.ToResponse(pageRes.Value));
    }
}
