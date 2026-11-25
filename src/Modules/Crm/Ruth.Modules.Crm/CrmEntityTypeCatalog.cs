using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.Assessments;
using Ruth.Modules.Crm.Domain.BackgroundChecks;
using Ruth.Modules.Crm.Domain.Badges;
using Ruth.Modules.Crm.Domain.Duplicates;
using Ruth.Modules.Crm.Domain.Households;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Domain.Relationships;
using Ruth.Modules.Crm.Domain.Signals;
using Ruth.SharedKernel.Metadata;

namespace Ruth.Modules.Crm;

internal static class CrmEntityTypeCatalog
{
    private static readonly EntityTypeRegistration[] _all =
    [
        Create(
            CrmSystemGuids.Person,
            Person.EntityTypeKeyName,
            nameof(Person),
            "Person",
            isSecured: true,
            supportsAttributes: true
        ),
        Create(
            CrmSystemGuids.Household,
            Household.EntityTypeKeyName,
            nameof(Household),
            "Household",
            isSecured: true,
            supportsAttributes: true
        ),
        Create(
            CrmSystemGuids.KnownRelationship,
            "Ruth.Crm.KnownRelationship",
            nameof(KnownRelationship),
            "Known Relationship",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CrmSystemGuids.SignalType,
            SignalType.EntityTypeKeyName,
            nameof(SignalType),
            "Signal Type",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CrmSystemGuids.PersonSignal,
            PersonSignal.EntityTypeKeyName,
            nameof(PersonSignal),
            "Person Signal",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CrmSystemGuids.BadgeDefinition,
            BadgeDefinition.EntityTypeKeyName,
            nameof(BadgeDefinition),
            "Badge Definition",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CrmSystemGuids.AssessmentType,
            AssessmentType.EntityTypeKeyName,
            nameof(AssessmentType),
            "Assessment Type",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CrmSystemGuids.Assessment,
            Assessment.EntityTypeKeyName,
            nameof(Assessment),
            "Assessment",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CrmSystemGuids.BackgroundCheck,
            BackgroundCheck.EntityTypeKeyName,
            nameof(BackgroundCheck),
            "Background Check",
            isSecured: true,
            supportsAttributes: false
        ),
        Create(
            CrmSystemGuids.PersonDuplicate,
            "Ruth.Crm.PersonDuplicate",
            nameof(PersonDuplicate),
            "Person Duplicate",
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
            "Crm",
            IsEntity: true,
            IsSecured: isSecured,
            SupportsAttributes: supportsAttributes
        );
    }
}
