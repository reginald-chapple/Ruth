using Ruth.Modules.Engagement.Contracts.SupportRequests;

namespace Ruth.Modules.Engagement.Domain.SupportRequests;

/// <summary>Intake inputs for creating a SupportRequest.</summary>
public sealed record SupportRequestIntakeSpec(
    string FirstName,
    string RequestText,
    string? LastName = null,
    string? Email = null,
    Guid? RequesterPersonId = null,
    Guid? BranchId = null,
    SupportVisibilityLevel VisibilityLevel = SupportVisibilityLevel.PrivateCareTeamOnly,
    bool IsUrgent = false,
    bool AllowComments = true
);
