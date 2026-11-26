using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Assistance;

/// <summary>Supporting intake or verification document attached to an <see cref="AssistanceCase" />.</summary>
public sealed class AssistanceDocument : Entity<AssistanceDocumentId>
{
    private AssistanceDocument() { }

    internal AssistanceDocument(
        AssistanceDocumentId id,
        AssistanceCaseId caseId,
        string title,
        string documentCategory,
        Guid? binaryFileId,
        Guid? uploadedByPersonId,
        bool isVerified,
        DateTimeOffset uploadedAtUtc
    )
        : base(id)
    {
        CaseId = caseId;
        Title = title;
        DocumentCategory = documentCategory;
        BinaryFileId = binaryFileId;
        UploadedByPersonId = uploadedByPersonId;
        IsVerified = isVerified;
        UploadedAtUtc = uploadedAtUtc.ToUniversalTime();
    }

    public AssistanceCaseId CaseId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string DocumentCategory { get; private set; } = string.Empty;
    public Guid? BinaryFileId { get; private set; }
    public Guid? UploadedByPersonId { get; private set; }
    public bool IsVerified { get; private set; }
    public DateTimeOffset UploadedAtUtc { get; private set; }
}
