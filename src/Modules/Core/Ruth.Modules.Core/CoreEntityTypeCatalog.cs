using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Domain.Collaboration;
using Ruth.Modules.Core.Domain.Files;
using Ruth.Modules.Core.Domain.Schedules;
using Ruth.SharedKernel.Metadata;

namespace Ruth.Modules.Core;

internal static class CoreEntityTypeCatalog
{
    private static readonly EntityTypeRegistration[] _all =
    [
        Create(
            CoreSystemGuids.EntityTypeDescriptor,
            "Ruth.Core.EntityTypeDescriptor",
            nameof(EntityTypeDescriptor),
            "Entity Type",
            isSecured: false,
            supportsAttributes: false
        ),
        Create(
            CoreSystemGuids.DefinedType,
            DefinedType.EntityTypeKeyName,
            nameof(DefinedType),
            "Defined Type",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CoreSystemGuids.DefinedValue,
            DefinedValue.EntityTypeKeyName,
            nameof(DefinedValue),
            "Defined Value",
            isSecured: true,
            supportsAttributes: true
        ),
        Create(
            CoreSystemGuids.Category,
            Category.EntityTypeKeyName,
            nameof(Category),
            "Category",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CoreSystemGuids.AttributeDefinition,
            AttributeDefinition.EntityTypeKeyName,
            nameof(AttributeDefinition),
            "Attribute Definition",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CoreSystemGuids.AttributeMatrixTemplate,
            AttributeMatrixTemplate.EntityTypeKeyName,
            nameof(AttributeMatrixTemplate),
            "Attribute Matrix Template",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CoreSystemGuids.Branch,
            Branch.EntityTypeKeyName,
            nameof(Branch),
            "Branch",
            isSecured: true,
            supportsAttributes: true
        ),
        Create(
            CoreSystemGuids.Location,
            Location.EntityTypeKeyName,
            nameof(Location),
            "Location",
            isSecured: true,
            supportsAttributes: true
        ),
        Create(
            CoreSystemGuids.Device,
            Device.EntityTypeKeyName,
            nameof(Device),
            "Device",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CoreSystemGuids.Schedule,
            Schedule.EntityTypeKeyName,
            nameof(Schedule),
            "Schedule",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CoreSystemGuids.BinaryFileType,
            BinaryFileType.EntityTypeKeyName,
            nameof(BinaryFileType),
            "Binary File Type",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CoreSystemGuids.BinaryFile,
            BinaryFile.EntityTypeKeyName,
            nameof(BinaryFile),
            "Binary File",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CoreSystemGuids.DocumentType,
            DocumentType.EntityTypeKeyName,
            nameof(DocumentType),
            "Document Type",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CoreSystemGuids.Document,
            Document.EntityTypeKeyName,
            nameof(Document),
            "Document",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CoreSystemGuids.SignatureDocumentTemplate,
            SignatureDocumentTemplate.EntityTypeKeyName,
            nameof(SignatureDocumentTemplate),
            "Signature Document Template",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CoreSystemGuids.SignatureDocument,
            SignatureDocument.EntityTypeKeyName,
            nameof(SignatureDocument),
            "Signature Document",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CoreSystemGuids.HistoryLogEntry,
            HistoryLogEntry.EntityTypeKeyName,
            nameof(HistoryLogEntry),
            "History Log Entry",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CoreSystemGuids.NoteType,
            NoteType.EntityTypeKeyName,
            nameof(NoteType),
            "Note Type",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CoreSystemGuids.Note,
            Note.EntityTypeKeyName,
            nameof(Note),
            "Note",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CoreSystemGuids.Tag,
            Tag.EntityTypeKeyName,
            nameof(Tag),
            "Tag",
            isSecured: true,
            supportsAttributes: false
        ),
    ];

    public static EntityTypeRegistration[] GetAll()
    {
        return _all;
    }

    private static EntityTypeRegistration Create(
        Guid systemGuid,
        string key,
        string name,
        string friendlyName,
        bool isSecured,
        bool supportsAttributes
    )
    {
        return new EntityTypeRegistration(
            systemGuid,
            key,
            name,
            friendlyName,
            "Core",
            IsEntity: true,
            IsSecured: isSecured,
            SupportsAttributes: supportsAttributes
        );
    }
}
