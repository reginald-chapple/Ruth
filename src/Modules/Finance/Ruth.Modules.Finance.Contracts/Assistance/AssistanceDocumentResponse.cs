namespace Ruth.Modules.Finance.Contracts.Assistance;

/// <summary>Supporting intake or verification document attached to an assistance case.</summary>
public sealed record AssistanceDocumentResponse(
    Guid Id,
    Guid CaseId,
    string Title,
    string DocumentCategory,
    Guid? BinaryFileId,
    Guid? UploadedByPersonId,
    bool IsVerified,
    DateTimeOffset UploadedAtUtc
);
