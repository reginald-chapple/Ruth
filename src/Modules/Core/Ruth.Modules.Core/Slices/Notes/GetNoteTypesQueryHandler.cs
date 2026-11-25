using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Notes;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Notes;

internal sealed class GetNoteTypesQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetNoteTypesQuery, IReadOnlyList<NoteTypeResponse>>
{
    public async Task<Result<IReadOnlyList<NoteTypeResponse>>> HandleAsync(
        GetNoteTypesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var baseQuery = dbContext.NoteTypes.AsNoTracking().AsQueryable();
        if (query.EntityTypeId.HasValue)
        {
            var etId = EntityTypeDescriptorId.From(query.EntityTypeId.Value);
            baseQuery = baseQuery.Where(t => t.EntityTypeId == etId);
        }

        var items = await baseQuery
            .OrderBy(static t => t.Order)
            .ThenBy(static t => t.Name)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<NoteTypeResponse>>([
            .. items.Select(NoteMapper.ToResponse),
        ]);
    }
}
