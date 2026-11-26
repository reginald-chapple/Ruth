using Ruth.Modules.Engagement.Domain;
using Ruth.Modules.Engagement.Domain.Achievements;
using Ruth.Modules.Engagement.Domain.Journeys;
using Ruth.Modules.Engagement.Domain.Pipelines;
using Ruth.Modules.Engagement.Domain.Reminders;
using Ruth.Modules.Engagement.Domain.Streaks;
using Ruth.Modules.Engagement.Domain.SupportRequests;
using Ruth.Modules.Engagement.Domain.Touchpoints;
using Ruth.SharedKernel.Metadata;

namespace Ruth.Modules.Engagement;

internal static class EngagementEntityTypeCatalog
{
    private static readonly EntityTypeRegistration[] _all =
    [
        Create(
            EngagementSystemGuids.PipelineType,
            PipelineType.EntityTypeKeyName,
            nameof(PipelineType),
            "Engagement Pipeline Type",
            isSecured: true,
            supportsAttributes: true
        ),
        Create(
            EngagementSystemGuids.EngagementOpportunity,
            EngagementOpportunity.EntityTypeKeyName,
            nameof(EngagementOpportunity),
            "Engagement Opportunity",
            isSecured: true,
            supportsAttributes: true
        ),
        Create(
            EngagementSystemGuids.EngagementRequest,
            EngagementRequest.EntityTypeKeyName,
            nameof(EngagementRequest),
            "Engagement Request",
            isSecured: true,
            supportsAttributes: true
        ),
        Create(
            EngagementSystemGuids.JourneyProgram,
            JourneyProgram.EntityTypeKeyName,
            nameof(JourneyProgram),
            "Journey Program",
            isSecured: true,
            supportsAttributes: true
        ),
        Create(
            EngagementSystemGuids.PersonMilestone,
            PersonMilestone.EntityTypeKeyName,
            nameof(PersonMilestone),
            "Person Milestone",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            EngagementSystemGuids.StreakType,
            StreakType.EntityTypeKeyName,
            nameof(StreakType),
            "Streak Type",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            EngagementSystemGuids.AchievementType,
            AchievementType.EntityTypeKeyName,
            nameof(AchievementType),
            "Achievement Type",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            EngagementSystemGuids.SupportCategory,
            SupportCategory.EntityTypeKeyName,
            nameof(SupportCategory),
            "Support Category",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            EngagementSystemGuids.SupportRequest,
            SupportRequest.EntityTypeKeyName,
            nameof(SupportRequest),
            "Support Request",
            isSecured: true,
            supportsAttributes: true
        ),
        Create(
            EngagementSystemGuids.ReminderType,
            ReminderType.EntityTypeKeyName,
            nameof(ReminderType),
            "Reminder Type",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            EngagementSystemGuids.ConstituentContact,
            ConstituentContact.EntityTypeKeyName,
            nameof(ConstituentContact),
            "Constituent Portfolio Contact",
            isSecured: true,
            supportsAttributes: true
        ),
        Create(
            EngagementSystemGuids.Touchpoint,
            Touchpoint.EntityTypeKeyName,
            nameof(Touchpoint),
            "Portfolio Touchpoint",
            isSecured: true,
            supportsAttributes: false
        ),
    ];

    public static EntityTypeRegistration[] GetAll()
    {
        return _all;
    }

    private static EntityTypeRegistration Create(
        Guid systemGuid,
        string key,
        string name,
        string friendlyName,
        bool isSecured,
        bool supportsAttributes
    )
    {
        return new EntityTypeRegistration(
            systemGuid,
            key,
            name,
            friendlyName,
            "Engagement",
            IsEntity: true,
            IsSecured: isSecured,
            SupportsAttributes: supportsAttributes
        );
    }
}
