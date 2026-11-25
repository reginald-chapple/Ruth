using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Domain.Files;

namespace Ruth.Modules.Core.Slices.Files;

internal static class FileMapper
{
    public static BinaryFileTypeResponse ToResponse(BinaryFileType t)
    {
        return new BinaryFileTypeResponse(
            t.Id.Value,
            t.SystemGuid,
            t.Name,
            t.Description,
            t.IconName,
            t.StorageProviderKey,
            t.RequiresViewSecurity,
            t.AllowAnonymous,
            t.PreferredRequired,
            t.MaxFileSizeBytes,
            t.MaxWidth,
            t.MaxHeight,
            t.AllowedExtensions,
            t.CacheControlHeaderSettings,
            t.IsSystem,
            t.CreatedOnUtc,
            t.ModifiedOnUtc
        );
    }

    public static BinaryFileResponse ToResponse(BinaryFile f)
    {
        return new BinaryFileResponse(
            f.Id.Value,
            f.SystemGuid,
            f.BinaryFileTypeId.Value,
            f.FileName,
            f.MimeType,
            f.FileSize,
            f.StorageProviderKey,
            f.ObjectKey,
            f.Width,
            f.Height,
            f.IsTemporary,
            f.IsSystem,
            f.Description,
            f.ParentEntityTypeId?.Value,
            f.ParentEntityId,
            f.UploadedByUserId,
            f.ContentLastModifiedOnUtc,
            f.CreatedOnUtc,
            f.ModifiedOnUtc
        );
    }

    public static DocumentTypeResponse ToResponse(DocumentType d)
    {
        return new DocumentTypeResponse(
            d.Id.Value,
            d.SystemGuid,
            d.Name,
            d.EntityTypeId.Value,
            d.EntityTypeQualifierColumn,
            d.EntityTypeQualifierValue,
            d.BinaryFileTypeId.Value,
            d.UserSelectable,
            d.IconName,
            d.Order,
            d.DefaultDocumentNameTemplate,
            d.MaxDocumentsPerEntity,
            d.IsImage,
            d.IsSystem,
            d.CreatedOnUtc,
            d.ModifiedOnUtc
        );
    }

    public static DocumentResponse ToResponse(Document d)
    {
        return new DocumentResponse(
            d.Id.Value,
            d.DocumentTypeId.Value,
            d.EntityId,
            d.BinaryFileId.Value,
            d.Name,
            d.PurposeKey,
            d.Description,
            d.IsSystem,
            d.CreatedOnUtc,
            d.ModifiedOnUtc
        );
    }

    public static SignatureDocumentTemplateResponse ToResponse(SignatureDocumentTemplate t)
    {
        return new SignatureDocumentTemplateResponse(
            t.Id.Value,
            t.SystemGuid,
            t.Name,
            t.Description,
            t.BinaryFileTypeId?.Value,
            t.DocumentTerm,
            t.LavaTemplate,
            t.SignatureType,
            t.IsActive,
            t.IsValidInFuture,
            t.ValidityDurationInDays,
            t.CreatedOnUtc,
            t.ModifiedOnUtc
        );
    }

    public static SignatureDocumentResponse ToResponse(SignatureDocument d)
    {
        return new SignatureDocumentResponse(
            d.Id.Value,
            d.SignatureDocumentTemplateId.Value,
            d.Name,
            d.DocumentKey,
            d.Status,
            d.AppliesToEntityId,
            d.AssignedToUserId,
            d.BinaryFileId?.Value,
            d.SignedByUserId,
            d.SignedName,
            d.SignedByEmail,
            d.SignedDocumentText,
            d.SignedClientIp,
            d.SignedClientUserAgent,
            d.SignedOnUtc,
            d.SignatureVerificationHash,
            !string.IsNullOrWhiteSpace(d.SignatureData),
            d.CreatedOnUtc,
            d.ModifiedOnUtc
        );
    }
}
