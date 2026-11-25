using Ruth.Modules.Communication.Domain;
using Ruth.Modules.Communication.Domain.Templates;
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
    ];

    public static EntityTypeRegistration[] GetAll()
    {
        return _all;
    }
}
