using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Contracts.Bookmarks;
using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Cms.Slices.Bookmarks;

internal sealed class AddPersonalLinkCommandHandler(CmsDbContext db)
    : ICommandHandler<AddPersonalLinkCommand, PersonalLinkSectionResponse>
{
    public async Task<Result<PersonalLinkSectionResponse>> HandleAsync(
        AddPersonalLinkCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var sectionId = PersonalLinkSectionId.From(command.SectionId);
        var section = await db
            .PersonalLinkSections.Include(static s => s.Links)
            .FirstOrDefaultAsync(s => s.Id == sectionId, cancellationToken);
        if (section is null)
        {
            return Result.Failure<PersonalLinkSectionResponse>(
                CmsErrors.NotFound("PersonalLinkSection", command.SectionId.ToString("D"))
            );
        }

        var linkRes = section.AddLink(command.Name, command.Url, command.IconName, command.Order);
        if (linkRes.IsFailure)
        {
            return Result.Failure<PersonalLinkSectionResponse>(linkRes.PrimaryError);
        }

        _ = db.PersonalLinks.Add(linkRes.Value);
        _ = await db.SaveChangesAsync(cancellationToken);
        return Result.Success(CmsContentProjectionMapper.ToResponse(section));
    }
}
