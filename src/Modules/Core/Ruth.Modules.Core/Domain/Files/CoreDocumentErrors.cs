using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.Files;

internal static class CoreDocumentErrors
{
    public static readonly DomainError BinaryFileTypeNameRequired = DomainError.Validation(
        "Core.BinaryFileTypeNameRequired",
        "Binary file type name is required."
    );

    public static readonly DomainError BinaryFileTypeNameTooLong = DomainError.Validation(
        "Core.BinaryFileTypeNameTooLong",
        "Binary file type name cannot exceed 100 characters."
    );

    public static readonly DomainError BinaryFileTypeDuplicateName = DomainError.Conflict(
        "Core.BinaryFileTypeDuplicateName",
        "A binary file type with that name already exists."
    );

    public static readonly DomainError BinaryFileTypeNotFound = DomainError.NotFound(
        "Core.BinaryFileTypeNotFound",
        "Binary file type was not found."
    );

    public static readonly DomainError BinaryFileTypeInUse = DomainError.Conflict(
        "Core.BinaryFileTypeInUse",
        "Cannot delete a binary file type that still has stored files."
    );

    public static readonly DomainError BinaryFileNotFound = DomainError.NotFound(
        "Core.BinaryFileNotFound",
        "Binary file was not found."
    );

    public static readonly DomainError BinaryFileNameInvalid = DomainError.Validation(
        "Core.BinaryFileNameInvalid",
        "File name is required and cannot use a blocked file extension."
    );

    public static readonly DomainError BinaryFileExtensionNotAllowed = DomainError.Validation(
        "Core.BinaryFileExtensionNotAllowed",
        "File extension is not allowed by this binary file type."
    );

    public static readonly DomainError BinaryFileTooLarge = DomainError.Validation(
        "Core.BinaryFileTooLarge",
        "Uploaded file exceeds the maximum allowed size for this binary file type."
    );

    public static readonly DomainError BinaryFileNotAnImage = DomainError.Validation(
        "Core.BinaryFileNotAnImage",
        "Image crop and resize operations require a supported raster image file."
    );

    public static readonly DomainError ImageCropBoundsInvalid = DomainError.Validation(
        "Core.ImageCropBoundsInvalid",
        "Image crop rectangle or target dimensions are invalid."
    );

    public static readonly DomainError StorageProviderNotFound = DomainError.Validation(
        "Core.StorageProviderNotFound",
        "Configured storage provider is not registered."
    );

    public static readonly DomainError DocumentTypeNameRequired = DomainError.Validation(
        "Core.DocumentTypeNameRequired",
        "Document type name is required."
    );

    public static readonly DomainError DocumentTypeNotFound = DomainError.NotFound(
        "Core.DocumentTypeNotFound",
        "Document type was not found."
    );

    public static readonly DomainError DocumentNameRequired = DomainError.Validation(
        "Core.DocumentNameRequired",
        "Document name is required."
    );

    public static readonly DomainError DocumentNotFound = DomainError.NotFound(
        "Core.DocumentNotFound",
        "Document was not found."
    );

    public static readonly DomainError DocumentQuotaExceeded = DomainError.Conflict(
        "Core.DocumentQuotaExceeded",
        "The entity has reached the maximum number of documents allowed for this document type."
    );

    public static readonly DomainError BinaryFileOwnershipDenied = DomainError.Forbidden(
        "Core.BinaryFileOwnershipDenied",
        "Only temporary files uploaded by the current user may be attached."
    );

    public static readonly DomainError SignatureTemplateNameRequired = DomainError.Validation(
        "Core.SignatureTemplateNameRequired",
        "Signature document template name is required."
    );

    public static readonly DomainError SignatureTemplateNotFound = DomainError.NotFound(
        "Core.SignatureTemplateNotFound",
        "Signature document template was not found."
    );

    public static readonly DomainError SignatureDocumentNotFound = DomainError.NotFound(
        "Core.SignatureDocumentNotFound",
        "Signature document was not found."
    );

    public static readonly DomainError SignatureDocumentAlreadySigned = DomainError.Conflict(
        "Core.SignatureDocumentAlreadySigned",
        "Signed signature documents are immutable and cannot be modified or re-signed."
    );

    public static readonly DomainError SignatureSignedNameRequired = DomainError.Validation(
        "Core.SignatureSignedNameRequired",
        "Signer legal name is required."
    );

    public static readonly DomainError SignatureDrawnDataInvalid = DomainError.Validation(
        "Core.SignatureDrawnDataInvalid",
        "Drawn signatures must be a valid base64-encoded PNG data URI."
    );
}
