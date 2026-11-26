using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Domain errors for the Engagement bounded context across W5-S1 through W5-S4.</summary>
public static class EngagementErrors
{
    public static readonly DomainError PipelineTypeInvalid = DomainError.Validation(
        "Engagement.PipelineTypeInvalid",
        "Pipeline type requires a valid name, slug, positive thresholds, and at least one initial stage."
    );

    public static readonly DomainError PipelineTypeNotFound = DomainError.NotFound(
        "Engagement.PipelineTypeNotFound",
        "Pipeline type was not found."
    );

    public static readonly DomainError PipelineSlugConflict = DomainError.Conflict(
        "Engagement.PipelineSlugConflict",
        "A pipeline type with the specified slug already exists."
    );

    public static readonly DomainError OpportunityInvalid = DomainError.Validation(
        "Engagement.OpportunityInvalid",
        "Engagement opportunity requires a valid pipeline type, name, and URL slug."
    );

    public static readonly DomainError OpportunityNotFound = DomainError.NotFound(
        "Engagement.OpportunityNotFound",
        "Engagement opportunity was not found."
    );

    public static readonly DomainError OpportunitySlugConflict = DomainError.Conflict(
        "Engagement.OpportunitySlugConflict",
        "An engagement opportunity with the specified slug already exists."
    );

    public static readonly DomainError EngagementRequestInvalid = DomainError.Validation(
        "Engagement.RequestInvalid",
        "Engagement request requires a valid opportunity, requester person, and initial stage."
    );

    public static readonly DomainError EngagementRequestNotFound = DomainError.NotFound(
        "Engagement.RequestNotFound",
        "Engagement request was not found."
    );

    public static readonly DomainError InvalidStageTransition = DomainError.Conflict(
        "Engagement.InvalidStageTransition",
        "Target stage does not belong to this pipeline or violates sequential stage progression."
    );

    public static readonly DomainError PlacementGroupRequired = DomainError.Conflict(
        "Engagement.PlacementGroupRequired",
        "This pipeline requires a placement group before marking an engagement request connected."
    );

    public static readonly DomainError FutureFollowUpNotAllowed = DomainError.Validation(
        "Engagement.FutureFollowUpNotAllowed",
        "Future follow-up requires future follow-ups enabled on the pipeline and a date strictly after today."
    );

    public static readonly DomainError JourneyProgramInvalid = DomainError.Validation(
        "Engagement.JourneyProgramInvalid",
        "Journey program requires a valid name, slug, at least one complete status, and acyclic milestone prerequisites."
    );

    public static readonly DomainError JourneyProgramNotFound = DomainError.NotFound(
        "Engagement.JourneyProgramNotFound",
        "Journey program was not found."
    );

    public static readonly DomainError JourneySlugConflict = DomainError.Conflict(
        "Engagement.JourneySlugConflict",
        "A journey program with the specified slug already exists."
    );

    public static readonly DomainError MilestonePrerequisiteUnmet = DomainError.Conflict(
        "Engagement.MilestonePrerequisiteUnmet",
        "All prerequisite milestones must be completed before completing this milestone."
    );

    public static readonly DomainError MilestoneDuplicateNotAllowed = DomainError.Conflict(
        "Engagement.MilestoneDuplicateNotAllowed",
        "This milestone type does not allow multiple completions for the same person."
    );

    public static readonly DomainError PersonMilestoneInvalid = DomainError.Validation(
        "Engagement.PersonMilestoneInvalid",
        "Person milestone requires a valid person, program, milestone type, and milestone status."
    );

    public static readonly DomainError StreakTypeInvalid = DomainError.Validation(
        "Engagement.StreakTypeInvalid",
        "Streak type requires a valid name, code, and anchor start date."
    );

    public static readonly DomainError StreakTypeNotFound = DomainError.NotFound(
        "Engagement.StreakTypeNotFound",
        "Streak type was not found."
    );

    public static readonly DomainError StreakCodeConflict = DomainError.Conflict(
        "Engagement.StreakCodeConflict",
        "A streak type with the specified code already exists."
    );

    public static readonly DomainError StreakDateBeforeAnchor = DomainError.Validation(
        "Engagement.StreakDateBeforeAnchor",
        "Engagement or exclusion date cannot precede the streak type anchor start date."
    );

    public static readonly DomainError StreakNotFound = DomainError.NotFound(
        "Engagement.StreakNotFound",
        "Streak enrollment was not found."
    );

    public static readonly DomainError AchievementTypeInvalid = DomainError.Validation(
        "Engagement.AchievementTypeInvalid",
        "Achievement type requires a valid name, code, and target count of at least 1."
    );

    public static readonly DomainError AchievementTypeNotFound = DomainError.NotFound(
        "Engagement.AchievementTypeNotFound",
        "Achievement type was not found."
    );

    public static readonly DomainError AchievementCodeConflict = DomainError.Conflict(
        "Engagement.AchievementCodeConflict",
        "An achievement type with the specified code already exists."
    );

    public static readonly DomainError AchievementPrerequisiteOrCapBlocked = DomainError.Conflict(
        "Engagement.AchievementPrerequisiteOrCapBlocked",
        "Achievement prerequisites are unmet or the maximum allowed successes have already been reached."
    );

    public static readonly DomainError SupportCategoryInvalid = DomainError.Validation(
        "Engagement.SupportCategoryInvalid",
        "Support category requires a valid name, slug, and expiration window between 1 and 365 days."
    );

    public static readonly DomainError SupportCategoryNotFound = DomainError.NotFound(
        "Engagement.SupportCategoryNotFound",
        "Support category was not found."
    );

    public static readonly DomainError SupportCategorySlugConflict = DomainError.Conflict(
        "Engagement.SupportCategorySlugConflict",
        "A support category with the specified slug already exists."
    );

    public static readonly DomainError SupportRequestInvalid = DomainError.Validation(
        "Engagement.SupportRequestInvalid",
        "Support request requires valid category, requester details, and non-empty request text."
    );

    public static readonly DomainError SupportRequestNotFound = DomainError.NotFound(
        "Engagement.SupportRequestNotFound",
        "Support request was not found."
    );

    public static readonly DomainError SupportRequestActionNotAllowed = DomainError.Conflict(
        "Engagement.SupportRequestActionNotAllowed",
        "The requested community support or comment operation is not permitted on this support request."
    );

    public static readonly DomainError ReminderTypeInvalid = DomainError.Validation(
        "Engagement.ReminderTypeInvalid",
        "Reminder type requires a valid name and highlight color."
    );

    public static readonly DomainError ReminderTypeNotFound = DomainError.NotFound(
        "Engagement.ReminderTypeNotFound",
        "Reminder type was not found."
    );

    public static readonly DomainError ReminderInvalid = DomainError.Validation(
        "Engagement.ReminderInvalid",
        "Reminder requires a valid reminder type, assignee person, target entity, and note."
    );

    public static readonly DomainError ReminderNotFound = DomainError.NotFound(
        "Engagement.ReminderNotFound",
        "Reminder was not found."
    );

    public static readonly DomainError NotificationNotFound = DomainError.NotFound(
        "Engagement.NotificationNotFound",
        "Notification message was not found."
    );

    public static readonly DomainError FollowingInvalid = DomainError.Validation(
        "Engagement.FollowingInvalid",
        "Following subscription requires a valid follower person, entity type key, and entity ID."
    );

    public static readonly DomainError ConstituentContactInvalid = DomainError.Validation(
        "Engagement.ConstituentContactInvalid",
        "Constituent contact requires distinct non-empty owner and constituent person IDs and valid cadence settings."
    );

    public static readonly DomainError ConstituentContactNotFound = DomainError.NotFound(
        "Engagement.ConstituentContactNotFound",
        "Constituent contact was not found."
    );

    public static readonly DomainError ConstituentContactConflict = DomainError.Conflict(
        "Engagement.ConstituentContactConflict",
        "This constituent is already in the specified owner's active portfolio."
    );

    public static readonly DomainError TouchpointInvalid = DomainError.Validation(
        "Engagement.TouchpointInvalid",
        "Touchpoint requires a valid constituent contact, channel, and scheduled date."
    );

    public static readonly DomainError TouchpointNotFound = DomainError.NotFound(
        "Engagement.TouchpointNotFound",
        "Touchpoint was not found."
    );

    public static readonly DomainError TouchpointAlreadyClosed = DomainError.Conflict(
        "Engagement.TouchpointAlreadyClosed",
        "Closed touchpoints cannot be modified."
    );
}
