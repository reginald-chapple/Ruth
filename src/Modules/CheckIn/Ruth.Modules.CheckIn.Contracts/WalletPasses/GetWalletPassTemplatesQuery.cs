using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Lists smart wallet pass templates ordered by name.</summary>
/// <param name="ActiveOnly">When true, returns only active templates.</param>
public sealed record GetWalletPassTemplatesQuery(bool ActiveOnly = false)
    : IQuery<IReadOnlyList<WalletPassTemplateResponse>>;
