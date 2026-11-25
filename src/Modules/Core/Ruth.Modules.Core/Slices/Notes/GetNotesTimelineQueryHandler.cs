using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Notes;
using Ruth.Modules.Core.Domain.Collaboration;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Notes;

internal sealed class GetNotesTimelineQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetNotesTimelineQuery, IReadOnlyList<NoteResponse>>
{
    public async Task<Result<IReadOnlyList<NoteResponse>>> HandleAsync(
        GetNotesTimelineQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var baseQuery = dbContext
            .Notes.AsNoTracking()
            .Include(static n => n.Attachments)
            .Where(n => n.EntityId == query.EntityId);

        if (query.NoteTypeId.HasValue)
        {
            var ntId = NoteTypeId.From(query.NoteTypeId.Value);
            baseQuery = baseQuery.Where(n => n.NoteTypeId == ntId);
        }

        var all = await baseQuery.ToListAsync(cancellationToken);
        var visible = all.Where(n => n.IsVisibleTo(query.ViewerUserId, query.IncludeUnapproved))
            .ToList();

        var byParent = visible
            .Where(static n => n.ParentNoteId.HasValue)
            .GroupBy(static n => n.ParentNoteId!.Value)
            .ToDictionary(
                static g => g.Key,
                static g => g.OrderBy(static n => n.CreatedOnUtc).ToList()
            );

        var roots = visible
            .Where(static n => !n.ParentNoteId.HasValue)
            .OrderByDescending(static n => n.IsAlert)
            .ThenByDescending(static n => n.IsPinned)
            .ThenByDescending(static n => n.CreatedOnUtc)
            .Select(root => BuildNode(root, byParent))
            .ToList();

        return Result.Success<IReadOnlyList<NoteResponse>>(roots);
    }

    private static NoteResponse BuildNode(Note note, Dictionary<NoteId, List<Note>> byParent)
    {
        if (!byParent.TryGetValue(note.Id, out var children))
        {
            return NoteMapper.ToResponse(note, []);
        }

        var replies = children.Select(c => BuildNode(c, byParent)).ToList();
        return NoteMapper.ToResponse(note, replies);
    }
}
