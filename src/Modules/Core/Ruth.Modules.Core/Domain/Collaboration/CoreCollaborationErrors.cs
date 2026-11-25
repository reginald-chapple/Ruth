using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.Collaboration;

internal static class CoreCollaborationErrors
{
    public static readonly DomainError HistoryVerbRequired = DomainError.Validation(
        "Core.HistoryVerbRequired",
        "History log verb is required."
    );

    public static readonly DomainError NoteTypeNameRequired = DomainError.Validation(
        "Core.NoteTypeNameRequired",
        "Note type name is required."
    );

    public static readonly DomainError NoteTypeNotFound = DomainError.NotFound(
        "Core.NoteTypeNotFound",
        "Note type was not found."
    );

    public static readonly DomainError NoteNotFound = DomainError.NotFound(
        "Core.NoteNotFound",
        "Note was not found."
    );

    public static readonly DomainError NoteTextRequired = DomainError.Validation(
        "Core.NoteTextRequired",
        "Note text is required."
    );

    public static readonly DomainError NoteRepliesDisabled = DomainError.Validation(
        "Core.NoteRepliesDisabled",
        "Replies are not enabled for this note type."
    );

    public static readonly DomainError NoteReplyDepthExceeded = DomainError.Validation(
        "Core.NoteReplyDepthExceeded",
        "Maximum reply nesting depth has been reached for this note type."
    );

    public static readonly DomainError NoteParentMismatch = DomainError.Validation(
        "Core.NoteParentMismatch",
        "Reply parent note must belong to the same entity and note type."
    );

    public static readonly DomainError NoteAccessDenied = DomainError.Forbidden(
        "Core.NoteAccessDenied",
        "You do not have permission to modify or delete this note."
    );

    public static readonly DomainError NoteWatchNotAllowed = DomainError.Validation(
        "Core.NoteWatchNotAllowed",
        "Watching is not enabled for this note type."
    );

    public static readonly DomainError NoteWatchFilterRequired = DomainError.Validation(
        "Core.NoteWatchFilterRequired",
        "A note watch must target at least a note, note type, or entity type."
    );

    public static readonly DomainError NoteWatchNonOverridable = DomainError.Forbidden(
        "Core.NoteWatchNonOverridable",
        "Cannot unwatch because an active non-overridable watch exists for this scope."
    );

    public static readonly DomainError TagNameInvalid = DomainError.Validation(
        "Core.TagNameInvalid",
        "Tag name is required (max 100 chars) and cannot contain '<', '>', '%', or '&'."
    );

    public static readonly DomainError TagDuplicate = DomainError.Conflict(
        "Core.TagDuplicate",
        "A tag with that name already exists in this ownership and entity scope."
    );

    public static readonly DomainError TagNotFound = DomainError.NotFound(
        "Core.TagNotFound",
        "Tag was not found."
    );

    public static readonly DomainError TagAccessDenied = DomainError.Forbidden(
        "Core.TagAccessDenied",
        "Personal tags can only be viewed or modified by their owner."
    );

    public static readonly DomainError EntitySetNotFound = DomainError.NotFound(
        "Core.EntitySetNotFound",
        "Entity set was not found."
    );

    public static readonly DomainError PreferenceKeyInvalid = DomainError.Validation(
        "Core.PreferenceKeyInvalid",
        "Preference key is required and cannot exceed 250 characters."
    );

    public static readonly DomainError PreferenceNotFound = DomainError.NotFound(
        "Core.PreferenceNotFound",
        "Preference was not found."
    );
}
