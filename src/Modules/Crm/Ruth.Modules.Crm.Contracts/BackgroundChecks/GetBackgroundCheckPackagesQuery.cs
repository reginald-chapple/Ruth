using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.BackgroundChecks;

/// <summary>Query to list all registered background check providers and their packages.</summary>
public sealed record GetBackgroundCheckPackagesQuery(string? ProviderKey = null)
    : IQuery<IReadOnlyList<BackgroundCheckPackageResponse>>;
