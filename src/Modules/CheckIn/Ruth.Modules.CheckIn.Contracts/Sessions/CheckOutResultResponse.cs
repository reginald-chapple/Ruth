namespace Ruth.Modules.CheckIn.Contracts.Sessions;

/// <summary>Outcome of a check-out operation.</summary>
/// <param name="CheckedOutCount">Number of entries transitioned to CheckedOut.</param>
/// <param name="CheckedOutOnUtc">UTC check-out timestamp.</param>
/// <param name="Entries">Updated entry receipts.</param>
public sealed record CheckOutResultResponse(
    int CheckedOutCount,
    DateTimeOffset CheckedOutOnUtc,
    IReadOnlyList<CheckedInEntryReceiptDto> Entries
);
