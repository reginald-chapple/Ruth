using Ruth.Modules.Cms.Contracts.Bookmarks;
using Ruth.Modules.Cms.Domain.Bookmarks;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Bookmarks;

internal sealed class CreatePersonalLinkSectionCommandHandler(CmsDbContext db)
    : ICommandHandler<CreatePersonalLinkSectionCommand, PersonalLinkSectionResponse>
{
    public async Task<Result<PersonalLinkSectionResponse>> HandleAsync(
        CreatePersonalLinkSectionCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var res = PersonalLinkSection.Create(
            command.Name,
            command.PersonId,
            command.IsShared,
            command.Order
        );
        if (res.IsFailure)
        {
            return Result.Failure<PersonalLinkSectionResponse>(res.PrimaryError);
        }

        _ = db.PersonalLinkSections.Add(res.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsContentProjectionMapper.ToResponse(res.Value));
    }
}
