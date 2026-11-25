using Ruth.Modules.Core.Contracts.Notes;
using Ruth.Modules.Core.Domain.Collaboration;

namespace Ruth.Modules.Core.Slices.Notes;

internal static class NoteMapper
{
    public static NoteTypeResponse ToResponse(NoteType t)
    {
        return new NoteTypeResponse(
            t.Id.Value,
            t.SystemGuid,
            t.EntityTypeId.Value,
            t.Name,
            t.UserSelectable,
            t.IconName,
            t.Color,
            t.Order,
            t.RequiresApprovals,
            t.AllowsWatching,
            t.AllowsReplies,
            t.MaxReplyDepth,
            t.AutoWatchAuthors,
            t.AllowsAttachments,
            t.BinaryFileTypeId?.Value,
            t.IsMentionEnabled,
            t.IsSystem,
            t.CreatedOnUtc,
            t.ModifiedOnUtc
        );
    }

    public static NoteResponse ToResponse(Note n, IReadOnlyList<NoteResponse>? replies = null)
    {
        return new NoteResponse(
            n.Id.Value,
            n.NoteTypeId.Value,
            n.EntityId,
            n.ParentNoteId?.Value,
            n.Caption,
            n.Text,
            n.IsAlert,
            n.IsPinned,
            n.IsPrivateNote,
            n.ApprovalStatus,
            n.ApprovedOnUtc,
            n.AuthorUserId,
            n.EditedOnUtc,
            [.. n.Attachments.Select(static a => a.BinaryFileId.Value)],
            replies ?? [],
            n.CreatedOnUtc
        );
    }

    public static NoteWatchResponse ToResponse(NoteWatch w)
    {
        return new NoteWatchResponse(
            w.Id.Value,
            w.NoteTypeId?.Value,
            w.EntityTypeId?.Value,
            w.EntityId,
            w.NoteId?.Value,
            w.WatcherUserId,
            w.IsWatching,
            w.WatchReplies,
            w.AllowOverride,
            w.CreatedOnUtc
        );
    }
}
