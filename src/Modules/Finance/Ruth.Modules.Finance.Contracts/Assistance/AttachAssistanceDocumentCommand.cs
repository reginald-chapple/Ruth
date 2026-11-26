using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Assistance;

/// <summary>Command to attach a supporting verification document to an assistance case.</summary>
public sealed record AttachAssistanceDocumentCommand(
    Guid CaseId,
    string Title,
    string DocumentCategory,
    Guid? BinaryFileId = null,
    Guid? UploadedByPersonId = null,
    bool IsVerified = false
) : ICommand<AssistanceCaseResponse>;
