using Ruth.Modules.Core.Contracts.EntitySets;
using Ruth.Modules.Core.Domain.Collaboration;

namespace Ruth.Modules.Core.Slices.EntitySets;

internal static class EntitySetMapper
{
    public static EntitySetResponse ToResponse(EntitySet set, DateTimeOffset nowUtc)
    {
        return new EntitySetResponse(
            set.Id.Value,
            set.ParentEntitySetId?.Value,
            set.Name,
            set.EntityTypeId?.Value,
            set.ExpireOnUtc,
            set.IsExpiredAt(nowUtc),
            set.PurposeValueId,
            set.Note,
            set.Order,
            [
                .. set
                    .Items.OrderBy(static i => i.Order)
                    .Select(static i => new EntitySetItemDto(
                        i.EntityId,
                        i.Order,
                        i.AdditionalMergeValuesJson
                    )),
            ],
            set.CreatedOnUtc
        );
    }
}
