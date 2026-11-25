using Ruth.Modules.Core.Contracts.Tags;
using Ruth.Modules.Core.Domain.Collaboration;

namespace Ruth.Modules.Core.Slices.Tags;

internal static class TagMapper
{
    public static TagResponse ToResponse(Tag tag, int? explicitCount = null)
    {
        return new TagResponse(
            tag.Id.Value,
            tag.SystemGuid,
            tag.EntityTypeId?.Value,
            tag.Name,
            tag.Description,
            tag.CategoryId?.Value,
            tag.OwnerUserId,
            tag.IsPersonal,
            tag.IconName,
            tag.BackgroundColor,
            tag.Order,
            tag.IsActive,
            tag.IsSystem,
            explicitCount ?? tag.TaggedItems.Count,
            tag.CreatedOnUtc
        );
    }
}
