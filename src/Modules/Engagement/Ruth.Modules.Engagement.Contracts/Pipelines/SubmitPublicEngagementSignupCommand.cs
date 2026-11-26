using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Pipelines;

/// <summary>Public signup command from the Opportunity Finder resolving or creating a CRM constituent.</summary>
/// <param name="OpportunityIdOrSlug">Target opportunity GUID or slug.</param>
/// <param name="FirstName">Requester first name.</param>
/// <param name="LastName">Requester last name.</param>
/// <param name="Email">Requester email address.</param>
/// <param name="MobilePhone">Optional requester mobile phone.</param>
/// <param name="ExistingPersonId">Optional known CRM Person identifier.</param>
/// <param name="BranchId">Optional preferred Branch identifier.</param>
/// <param name="Comments">Optional signup message.</param>
public sealed record SubmitPublicEngagementSignupCommand(
    string OpportunityIdOrSlug,
    string FirstName,
    string LastName,
    string Email,
    string? MobilePhone = null,
    Guid? ExistingPersonId = null,
    Guid? BranchId = null,
    string? Comments = null
) : ICommand<EngagementRequestResponse>;
