using Ruth.Modules.Core.Contracts.Notes;
using Ruth.Modules.Core.Domain.Files;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Core.Domain.Collaboration;

/// <summary>Threaded note aggregate with privacy isolation, approval workflow, pinning, alerts, and attachments.</summary>
public sealed class Note : AggregateRoot<NoteId>, IAuditableEntity, IHasConcurrencyToken, ISecured
{
    /// <summary>Canonical entity type key for Note.</summary>
    public const string EntityTypeKeyName = "Ruth.Core.Note";

    /// <summary>Default automatic caption for personal private notes.</summary>
    public const string PersonalNoteDefaultCaption = "You - Personal Note";

    private readonly List<NoteAttachment> _attachments = [];

    private Note() { }

    private Note(
        NoteId id,
        NoteTypeId noteTypeId,
        Guid entityId,
        NoteId? parentNoteId,
        string? caption,
        string text,
        bool isAlert,
        bool isPinned,
        bool isPrivateNote,
        NoteApprovalState approvalStatus,
        DateTimeOffset? approvedOnUtc,
        Guid? authorUserId
    )
        : base(id)
    {
        NoteTypeId = noteTypeId;
        EntityId = entityId;
        ParentNoteId = parentNoteId;
        Caption = caption;
        Text = text;
        IsAlert = isAlert;
        IsPinned = isPinned;
        IsPrivateNote = isPrivateNote;
        ApprovalStatus = approvalStatus;
        ApprovedOnUtc = approvedOnUtc;
        AuthorUserId = authorUserId;
    }

    /// <summary>Gets the owning note type identifier.</summary>
    public NoteTypeId NoteTypeId { get; private set; }

    /// <summary>Gets the target entity identifier this note is attached to.</summary>
    public Guid EntityId { get; private set; }

    /// <summary>Gets the optional parent note identifier for threaded replies.</summary>
    public NoteId? ParentNoteId { get; private set; }

    /// <summary>Gets the optional note caption.</summary>
    public string? Caption { get; private set; }

    /// <summary>Gets the note body text.</summary>
    public string Text { get; private set; } = string.Empty;

    /// <summary>Gets whether this note is highlighted as a top-priority alert.</summary>
    public bool IsAlert { get; private set; }

    /// <summary>Gets whether this note is pinned near the top of the timeline.</summary>
    public bool IsPinned { get; private set; }

    /// <summary>Gets whether this note is visible exclusively to its author.</summary>
    public bool IsPrivateNote { get; private set; }

    /// <summary>Gets the approval moderation status.</summary>
    public NoteApprovalState ApprovalStatus { get; private set; }

    /// <summary>Gets when this note was approved.</summary>
    public DateTimeOffset? ApprovedOnUtc { get; private set; }

    /// <summary>Gets the author user account identifier.</summary>
    public Guid? AuthorUserId { get; private set; }

    /// <summary>Gets when the note text was last edited.</summary>
    public DateTimeOffset? EditedOnUtc { get; private set; }

    /// <summary>Gets the user who last edited the note.</summary>
    public Guid? EditedByUserId { get; private set; }

    /// <summary>Gets the binary file attachments.</summary>
    public IReadOnlyCollection<NoteAttachment> Attachments => _attachments.AsReadOnly();

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    /// <inheritdoc />
    public string EntityTypeKey => EntityTypeKeyName;

    /// <inheritdoc />
    Guid ISecured.EntityId => Id.Value;

    /// <inheritdoc />
    public ISecured ParentAuthority =>
        new SecuredEntityRef(
            NoteType.EntityTypeKeyName,
            NoteTypeId.Value,
            GlobalDefaultSecured.Instance
        );

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    /// <summary>Evaluates whether <paramref name="viewerUserId" /> can see this note.</summary>
    public bool IsVisibleTo(Guid? viewerUserId, bool includeUnapproved = false)
    {
        return IsPrivateNote
            ? viewerUserId.HasValue
                && AuthorUserId.HasValue
                && viewerUserId.Value == AuthorUserId.Value
            : ApprovalStatus == NoteApprovalState.Approved
                || includeUnapproved
                || (
                    ApprovalStatus == NoteApprovalState.PendingApproval
                    && viewerUserId.HasValue
                    && AuthorUserId.HasValue
                    && viewerUserId.Value == AuthorUserId.Value
                );
    }

    /// <summary>Checks whether a user can edit or delete this note (private notes are strictly author-only).</summary>
    public bool CanBeModifiedBy(Guid? requestingUserId, bool isAdmin)
    {
        return IsPrivateNote
            ? requestingUserId.HasValue
                && AuthorUserId.HasValue
                && requestingUserId.Value == AuthorUserId.Value
            : isAdmin
                || (
                    requestingUserId.HasValue
                    && AuthorUserId.HasValue
                    && requestingUserId.Value == AuthorUserId.Value
                );
    }

    /// <summary>Updates the approval status of this note.</summary>
    public void SetApprovalStatus(NoteApprovalState status, DateTimeOffset nowUtc)
    {
        ApprovalStatus = status;
        ApprovedOnUtc = status == NoteApprovalState.Approved ? nowUtc : null;
    }

    /// <summary>Attaches a binary file identifier to this note.</summary>
    public void AddAttachment(BinaryFileId binaryFileId)
    {
        if (_attachments.TrueForAll(a => a.BinaryFileId != binaryFileId))
        {
            _attachments.Add(new NoteAttachment(Guid.CreateVersion7(), Id, binaryFileId));
        }
    }

    /// <summary>Creates a validated note enforcing approval status and personal-note caption rules.</summary>
    public static Result<Note> Create(
        NoteType noteType,
        Guid entityId,
        string text,
        string? caption,
        NoteId? parentNoteId,
        bool isAlert,
        bool isPinned,
        bool isPrivateNote,
        Guid? authorUserId,
        DateTimeOffset nowUtc
    )
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return CoreCollaborationErrors.NoteTextRequired;
        }

        var normalizedCaption = string.IsNullOrWhiteSpace(caption) ? null : caption.Trim();
        if (isPrivateNote && normalizedCaption is null)
        {
            normalizedCaption = PersonalNoteDefaultCaption;
        }

        var approval = noteType.RequiresApprovals
            ? NoteApprovalState.PendingApproval
            : NoteApprovalState.Approved;
        DateTimeOffset? approvedAt = approval == NoteApprovalState.Approved ? nowUtc : null;

        return new Note(
            NoteId.Create(),
            noteType.Id,
            entityId,
            parentNoteId,
            normalizedCaption,
            text.Trim(),
            isAlert,
            isPinned,
            isPrivateNote,
            approval,
            approvedAt,
            authorUserId
        );
    }
}
