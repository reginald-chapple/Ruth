namespace Ruth.Modules.Crm.Contracts.BackgroundChecks;

/// <summary>Provider request payload issued when initiating a background check.</summary>
public sealed record BackgroundCheckSubmissionRequest(
    Guid BackgroundCheckId,
    Guid PersonId,
    string FirstName,
    string? MiddleName,
    string LastName,
    string? Email,
    string PackageSlug
);
