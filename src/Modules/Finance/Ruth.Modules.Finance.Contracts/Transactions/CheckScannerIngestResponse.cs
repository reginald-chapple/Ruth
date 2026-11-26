namespace Ruth.Modules.Finance.Contracts.Transactions;

/// <summary>Response returned after ingesting a scanned check item via the check-scanner pipeline.</summary>
/// <param name="Transaction">Created check financial transaction.</param>
/// <param name="BatchId">Open check-scanner batch into which the check was posted.</param>
/// <param name="MatchedPersonId">Auto-matched constituent Person identifier (if bank account hash was recognized).</param>
/// <param name="RoutingAndAccountHash">Keyed HMAC-SHA256 digest of the scanned MICR routing and account numbers.</param>
public sealed record CheckScannerIngestResponse(
    FinancialTransactionResponse Transaction,
    Guid BatchId,
    Guid? MatchedPersonId,
    string RoutingAndAccountHash
);
