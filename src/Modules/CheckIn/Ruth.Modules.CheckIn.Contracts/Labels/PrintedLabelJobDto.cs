namespace Ruth.Modules.CheckIn.Contracts.Labels;

/// <summary>Individual compiled badge or guardian receipt print job produced for a check-in session.</summary>
/// <param name="LabelId">Source label identifier.</param>
/// <param name="LabelName">Source label display name.</param>
/// <param name="LabelKind">Label functional role.</param>
/// <param name="EntryId">Optional associated check-in entry identifier.</param>
/// <param name="DispatchedFromServer">True if sent over server TCP raw socket; false if returned for browser WebUSB/local print.</param>
/// <param name="PrinterEndpoint">Optional resolved printer endpoint.</param>
/// <param name="BytesSent">Bytes transmitted when dispatched from server.</param>
/// <param name="CompiledZpl">Compiled Zebra ZPL II payload.</param>
/// <param name="Preview">Visual preview elements for client fallback printing.</param>
public sealed record PrintedLabelJobDto(
    Guid LabelId,
    string LabelName,
    CheckInLabelKind LabelKind,
    Guid? EntryId,
    bool DispatchedFromServer,
    string? PrinterEndpoint,
    int BytesSent,
    string CompiledZpl,
    CheckInLabelPreviewResponse Preview
);
