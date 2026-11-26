using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Sub-50ms batched CRM lookup for check-in kiosks resolving phone digits, barcodes/member cards, or names into households and member profiles.</summary>
/// <param name="SearchTerm">Raw phone digits, barcode/search key, or name query.</param>
/// <param name="AllowPhoneMatch">Whether phone suffix/number matching is enabled.</param>
/// <param name="AllowNameMatch">Whether name matching is enabled.</param>
/// <param name="AllowBarcodeMatch">Whether exact search-key/barcode matching is enabled.</param>
/// <param name="MaxHouseholds">Maximum households/groups to return (1..25).</param>
/// <param name="ExplicitPersonId">Optional pre-resolved person identifier (for example from a scanned WLT-* wallet pass).</param>
public sealed record LookupKioskHouseholdsQuery(
    string SearchTerm,
    bool AllowPhoneMatch = true,
    bool AllowNameMatch = true,
    bool AllowBarcodeMatch = true,
    int MaxHouseholds = 15,
    Guid? ExplicitPersonId = null
) : IQuery<IReadOnlyList<KioskHouseholdLookupResponse>>;
