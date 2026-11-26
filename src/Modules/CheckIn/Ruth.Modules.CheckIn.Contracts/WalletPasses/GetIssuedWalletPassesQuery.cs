using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Queries issued smart wallet passes filtered by person, template, or active status.</summary>
/// <param name="PersonId">Optional holder CRM person identifier filter.</param>
/// <param name="TemplateId">Optional template identifier filter.</param>
/// <param name="ActiveOnly">When true, returns only active non-revoked passes.</param>
public sealed record GetIssuedWalletPassesQuery(
    Guid? PersonId = null,
    Guid? TemplateId = null,
    bool ActiveOnly = false
) : IQuery<IReadOnlyList<IssuedWalletPassResponse>>;
