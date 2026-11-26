using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.SupportRequests;

/// <summary>Command to submit a new Community Care / SupportRequest.</summary>
/// <param name="SupportCategoryId">Target SupportCategory identifier.</param>
/// <param name="FirstName">Requester first name (or Anonymous).</param>
/// <param name="RequestText">Support request body.</param>
/// <param name="LastName">Optional last name.</param>
/// <param name="Email">Optional email.</param>
/// <param name="RequesterPersonId">Optional CRM Person identifier.</param>
/// <param name="BranchId">Optional Branch identifier.</param>
/// <param name="VisibilityLevel">Requested visibility scope.</param>
/// <param name="IsUrgent">Whether urgent.</param>
/// <param name="AllowComments">Whether comments are permitted.</param>
public sealed record SubmitSupportRequestCommand(
    Guid SupportCategoryId,
    string FirstName,
    string RequestText,
    string? LastName = null,
    string? Email = null,
    Guid? RequesterPersonId = null,
    Guid? BranchId = null,
    SupportVisibilityLevel VisibilityLevel = SupportVisibilityLevel.PrivateCareTeamOnly,
    bool IsUrgent = false,
    bool AllowComments = true
) : ICommand<SupportRequestResponse>;
