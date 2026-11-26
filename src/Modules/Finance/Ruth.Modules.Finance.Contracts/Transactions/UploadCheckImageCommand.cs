using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Transactions;

/// <summary>Command to encrypt a check or remittance image with AES-256-GCM and attach it to a financial transaction.</summary>
/// <param name="TransactionId">Target financial transaction identifier.</param>
/// <param name="ImageSide">Front, Back, or RemittanceSlip.</param>
/// <param name="ContentType">MIME type (for example, image/png or image/tiff).</param>
/// <param name="ImageBase64">Base64-encoded plaintext check image bytes.</param>
public sealed record UploadCheckImageCommand(
    Guid TransactionId,
    CheckImageSide ImageSide,
    string ContentType,
    string ImageBase64
) : ICommand<FinancialTransactionImageResponse>;
