using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Transactions;

/// <summary>Command to ingest a scanned paper check, match its hashed ABA routing/account to a constituent, and encrypt check images into RustFS.</summary>
/// <param name="RoutingNumber">9-digit ABA routing number read from the check MICR line.</param>
/// <param name="AccountNumber">Bank account number read from the check MICR line.</param>
/// <param name="CheckNumber">Check serial number.</param>
/// <param name="Amount">Positive check amount.</param>
/// <param name="CurrencyCode">3-letter ISO-4217 currency code.</param>
/// <param name="AccountId">Target financial account identifier.</param>
/// <param name="BatchId">Optional target open financial batch identifier (auto-creates/reuses an open scanner batch when null).</param>
/// <param name="OverridePersonId">Optional explicit constituent Person identifier when overriding or binding an unrecognized check.</param>
/// <param name="BindBankIdentifierToPerson">When true and a PersonId is known, binds the routing/account hash for future automatic matching.</param>
/// <param name="InvoiceId">Optional open invoice identifier settled by this check.</param>
/// <param name="FrontImageBase64">Optional base64 PNG/TIFF front check image.</param>
/// <param name="BackImageBase64">Optional base64 PNG/TIFF back check image.</param>
/// <param name="ImageContentType">MIME type for supplied check images.</param>
/// <param name="Summary">Optional check memo.</param>
public sealed record IngestScannedCheckCommand(
    string RoutingNumber,
    string AccountNumber,
    string CheckNumber,
    decimal Amount,
    string CurrencyCode,
    Guid AccountId,
    Guid? BatchId = null,
    Guid? OverridePersonId = null,
    bool BindBankIdentifierToPerson = true,
    Guid? InvoiceId = null,
    string? FrontImageBase64 = null,
    string? BackImageBase64 = null,
    string ImageContentType = "image/png",
    string? Summary = null
) : ICommand<CheckScannerIngestResponse>;
