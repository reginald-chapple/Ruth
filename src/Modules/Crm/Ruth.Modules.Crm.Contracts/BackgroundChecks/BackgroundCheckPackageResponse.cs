namespace Ruth.Modules.Crm.Contracts.BackgroundChecks;

/// <summary>Represents a screening package offered by a background check provider.</summary>
public sealed record BackgroundCheckPackageResponse(
    string ProviderKey,
    string Slug,
    string Name,
    string? Description
);
