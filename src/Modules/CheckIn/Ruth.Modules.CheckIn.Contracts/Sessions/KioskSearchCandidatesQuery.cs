using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.Sessions;

/// <summary>High-throughput kiosk and mobile search query evaluating household candidates, eligibility, and live room balancing.</summary>
/// <param name="ConfigurationId">Target check-in configuration profile identifier.</param>
/// <param name="ScheduleId">Active schedule identifier being checked into.</param>
/// <param name="SearchTerm">Phone digits, name text, or scanned barcode/card token.</param>
/// <param name="InputKind">Explicit or automatic input classification.</param>
/// <param name="ServiceDate">Optional local service date (defaults to current UTC date).</param>
/// <param name="MaxFamilies">Maximum household cards to return (1..25).</param>
public sealed record KioskSearchCandidatesQuery(
    Guid ConfigurationId,
    Guid ScheduleId,
    string SearchTerm,
    KioskSearchInputKind InputKind = KioskSearchInputKind.Auto,
    DateOnly? ServiceDate = null,
    int MaxFamilies = 15
) : IQuery<KioskSearchResponse>;
