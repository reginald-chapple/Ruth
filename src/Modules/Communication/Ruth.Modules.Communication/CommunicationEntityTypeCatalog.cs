using Ruth.Modules.Communication.Domain;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Domain.Flows;
using Ruth.Modules.Communication.Domain.PersonalDevices;
using Ruth.Modules.Communication.Domain.PhoneNumbers;
using Ruth.Modules.Communication.Domain.SystemCommunications;
using Ruth.Modules.Communication.Domain.Templates;
using Ruth.Modules.Communication.Domain.TwoWaySms;
using Ruth.SharedKernel.Metadata;

namespace Ruth.Modules.Communication;

internal static class CommunicationEntityTypeCatalog
{
    private static readonly EntityTypeRegistration[] _all =
    [
        new(
            CommunicationSystemGuids.TemplateSnippet,
            TemplateSnippet.EntityTypeKeyName,
            nameof(TemplateSnippet),
            "Template Snippet",
            "Communication",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
        new(
            CommunicationSystemGuids.SystemCommunication,
            SystemCommunication.EntityTypeKeyName,
            nameof(SystemCommunication),
            "System Communication",
            "Communication",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: true
        ),
        new(
            CommunicationSystemGuids.SystemPhoneNumber,
            SystemPhoneNumber.EntityTypeKeyName,
            nameof(SystemPhoneNumber),
            "System Phone Number",
            "Communication",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: true
        ),
        new(
            CommunicationSystemGuids.PersonalDevice,
            PersonalDevice.EntityTypeKeyName,
            nameof(PersonalDevice),
            "Personal Device",
            "Communication",
            IsEntity: true,
            IsSecured: false,
            SupportsAttributes: false
        ),
        new(
            CommunicationSystemGuids.CommunicationTemplate,
            CommunicationTemplate.EntityTypeKeyName,
            nameof(CommunicationTemplate),
            "Communication Template",
            "Communication",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
        new(
            CommunicationSystemGuids.Communication,
            Domain.BulkCommunications.Communication.EntityTypeKeyName,
            nameof(Domain.BulkCommunications.Communication),
            "Bulk Communication",
            "Communication",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
        new(
            CommunicationSystemGuids.SmsPipeline,
            SmsPipeline.EntityTypeKeyName,
            nameof(SmsPipeline),
            "SMS Keyword Pipeline",
            "Communication",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
        new(
            CommunicationSystemGuids.CommunicationResponse,
            CommunicationResponse.EntityTypeKeyName,
            nameof(CommunicationResponse),
            "SMS Conversation Response",
            "Communication",
            IsEntity: true,
            IsSecured: false,
            SupportsAttributes: false
        ),
        new(
            CommunicationSystemGuids.CommunicationFlow,
            CommunicationFlow.EntityTypeKeyName,
            nameof(CommunicationFlow),
            "Communication Drip Flow",
            "Communication",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
    ];

    public static EntityTypeRegistration[] GetAll()
    {
        return _all;
    }
}
