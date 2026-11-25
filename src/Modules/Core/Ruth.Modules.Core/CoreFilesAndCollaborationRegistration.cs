using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Core.Contracts.EntitySets;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Contracts.History;
using Ruth.Modules.Core.Contracts.Notes;
using Ruth.Modules.Core.Contracts.Preferences;
using Ruth.Modules.Core.Contracts.Storage;
using Ruth.Modules.Core.Contracts.Tags;
using Ruth.Modules.Core.Slices.EntitySets;
using Ruth.Modules.Core.Slices.Files;
using Ruth.Modules.Core.Slices.History;
using Ruth.Modules.Core.Slices.Notes;
using Ruth.Modules.Core.Slices.Preferences;
using Ruth.Modules.Core.Slices.Tags;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Core;

internal static class CoreFilesAndCollaborationRegistration
{
    public static void RegisterFilesHandlers(IServiceCollection services)
    {
        RegisterBinaryFileHandlers(services);
        RegisterDocumentAndSignatureHandlers(services);
    }

    public static void RegisterCollaborationHandlers(IServiceCollection services)
    {
        RegisterHistoryAndNoteHandlers(services);
        RegisterTagSetPreferenceHandlers(services);
    }

    private static void RegisterBinaryFileHandlers(IServiceCollection services)
    {
        _ = services.AddQueryHandler<
            GetBinaryFileTypesQuery,
            IReadOnlyList<BinaryFileTypeResponse>,
            GetBinaryFileTypesQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateBinaryFileTypeCommand,
            BinaryFileTypeResponse,
            CreateBinaryFileTypeCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateBinaryFileTypeCommand,
            BinaryFileTypeResponse,
            UpdateBinaryFileTypeCommandHandler
        >();
        _ = services.AddCommandHandler<
            DeleteBinaryFileTypeCommand,
            bool,
            DeleteBinaryFileTypeCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetBinaryFilesQuery,
            IReadOnlyList<BinaryFileResponse>,
            GetBinaryFilesQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetBinaryFileByIdQuery,
            BinaryFileResponse,
            GetBinaryFileByIdQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetBinaryFilePresignedUrlQuery,
            StoragePresignedUrl,
            GetBinaryFilePresignedUrlQueryHandler
        >();
        _ = services.AddCommandHandler<
            UploadBinaryFileCommand,
            BinaryFileResponse,
            UploadBinaryFileCommandHandler
        >();
        _ = services.AddCommandHandler<
            CropBinaryFileImageCommand,
            BinaryFileResponse,
            CropBinaryFileImageCommandHandler
        >();
        _ = services.AddCommandHandler<
            DeleteBinaryFileCommand,
            bool,
            DeleteBinaryFileCommandHandler
        >();
    }

    private static void RegisterDocumentAndSignatureHandlers(IServiceCollection services)
    {
        _ = services.AddQueryHandler<
            GetDocumentTypesQuery,
            IReadOnlyList<DocumentTypeResponse>,
            GetDocumentTypesQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateDocumentTypeCommand,
            DocumentTypeResponse,
            CreateDocumentTypeCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetDocumentsQuery,
            IReadOnlyList<DocumentResponse>,
            GetDocumentsQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateDocumentCommand,
            DocumentResponse,
            CreateDocumentCommandHandler
        >();
        _ = services.AddCommandHandler<DeleteDocumentCommand, bool, DeleteDocumentCommandHandler>();
        _ = services.AddQueryHandler<
            GetSignatureDocumentTemplatesQuery,
            IReadOnlyList<SignatureDocumentTemplateResponse>,
            GetSignatureDocumentTemplatesQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateSignatureDocumentTemplateCommand,
            SignatureDocumentTemplateResponse,
            CreateSignatureDocumentTemplateCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetSignatureDocumentsQuery,
            IReadOnlyList<SignatureDocumentResponse>,
            GetSignatureDocumentsQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateSignatureDocumentCommand,
            SignatureDocumentResponse,
            CreateSignatureDocumentCommandHandler
        >();
        _ = services.AddCommandHandler<
            SignSignatureDocumentCommand,
            SignatureDocumentResponse,
            SignSignatureDocumentCommandHandler
        >();
    }

    private static void RegisterHistoryAndNoteHandlers(IServiceCollection services)
    {
        _ = services.AddQueryHandler<
            GetHistoryLogQuery,
            IReadOnlyList<HistoryLogEntryResponse>,
            GetHistoryLogQueryHandler
        >();
        _ = services.AddCommandHandler<
            RecordHistoryLogCommand,
            HistoryLogEntryResponse,
            RecordHistoryLogCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetNoteTypesQuery,
            IReadOnlyList<NoteTypeResponse>,
            GetNoteTypesQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateNoteTypeCommand,
            NoteTypeResponse,
            CreateNoteTypeCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetNotesTimelineQuery,
            IReadOnlyList<NoteResponse>,
            GetNotesTimelineQueryHandler
        >();
        _ = services.AddCommandHandler<CreateNoteCommand, NoteResponse, CreateNoteCommandHandler>();
        _ = services.AddCommandHandler<
            SetNoteApprovalCommand,
            NoteResponse,
            SetNoteApprovalCommandHandler
        >();
        _ = services.AddCommandHandler<DeleteNoteCommand, bool, DeleteNoteCommandHandler>();
        _ = services.AddCommandHandler<
            SetNoteWatchCommand,
            NoteWatchResponse,
            SetNoteWatchCommandHandler
        >();
    }

    private static void RegisterTagSetPreferenceHandlers(IServiceCollection services)
    {
        _ = services.AddQueryHandler<
            GetTagsQuery,
            IReadOnlyList<TagResponse>,
            GetTagsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetEntityTagsQuery,
            IReadOnlyList<TagResponse>,
            GetEntityTagsQueryHandler
        >();
        _ = services.AddCommandHandler<CreateTagCommand, TagResponse, CreateTagCommandHandler>();
        _ = services.AddCommandHandler<TagEntityCommand, bool, TagEntityCommandHandler>();
        _ = services.AddCommandHandler<UntagEntityCommand, bool, UntagEntityCommandHandler>();
        _ = services.AddCommandHandler<DeleteTagCommand, bool, DeleteTagCommandHandler>();
        _ = services.AddQueryHandler<
            GetEntitySetsQuery,
            IReadOnlyList<EntitySetResponse>,
            GetEntitySetsQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateEntitySetCommand,
            EntitySetResponse,
            CreateEntitySetCommandHandler
        >();
        _ = services.AddCommandHandler<
            PurgeExpiredEntitySetsCommand,
            int,
            PurgeExpiredEntitySetsCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetPersonPreferencesQuery,
            IReadOnlyList<PersonPreferenceResponse>,
            GetPersonPreferencesQueryHandler
        >();
        _ = services.AddCommandHandler<
            SetPersonPreferenceCommand,
            PersonPreferenceResponse,
            SetPersonPreferenceCommandHandler
        >();
        _ = services.AddCommandHandler<
            DeletePersonPreferenceCommand,
            bool,
            DeletePersonPreferenceCommandHandler
        >();
    }
}
