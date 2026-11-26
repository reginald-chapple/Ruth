using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Lms.Contracts.Classes;
using Ruth.Modules.Lms.Domain;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Lms.Slices.Classes;

internal sealed class AddClassAnnouncementCommandHandler(LmsDbContext db, IClock clock)
    : ICommandHandler<AddClassAnnouncementCommand, LearningClassAnnouncementResponse>
{
    public async Task<Result<LearningClassAnnouncementResponse>> HandleAsync(
        AddClassAnnouncementCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var classId = LearningClassId.From(command.LearningClassId);
        var cls = await db
            .Classes.Include(static c => c.Announcements)
            .FirstOrDefaultAsync(c => c.Id == classId, cancellationToken);
        if (cls is null)
        {
            return Result.Failure<LearningClassAnnouncementResponse>(
                LmsErrors.NotFound("Class.NotFound", "LearningClass was not found.")
            );
        }

        var annRes = cls.AddAnnouncement(
            command.Title,
            command.DescriptionHtml,
            command.PublishDateTimeUtc ?? clock.UtcNow,
            command.Summary
        );
        if (annRes.IsFailure)
        {
            return Result.Failure<LearningClassAnnouncementResponse>(annRes.PrimaryError);
        }

        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(LmsDtoMapper.ToResponse(annRes.Value));
    }
}
