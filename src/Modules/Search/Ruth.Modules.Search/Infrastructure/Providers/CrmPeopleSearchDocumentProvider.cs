using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Search.Infrastructure.Providers;

internal sealed class CrmPeopleSearchDocumentProvider(IServiceProvider serviceProvider)
    : ISearchDocumentProvider
{
    public string ProviderKey => "crm.people";
    public string ModuleName => "Crm";
    public string EntityTypeKey => "Ruth.Crm.Person";

    public async Task<IReadOnlyList<SearchDocumentUpsertSpec>> LoadDocumentsAsync(
        SearchProviderLoadRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var handler = serviceProvider.GetService<
            IQueryHandler<SearchPeopleQuery, IReadOnlyList<PersonSummaryResponse>>
        >();
        if (handler is null)
        {
            return [];
        }

        var res = await handler.HandleAsync(
            new SearchPeopleQuery(
                IncludeDeceased: false,
                IncludeBusinesses: true,
                MaxResults: Math.Clamp(request.MaxDocuments, 1, 500)
            ),
            cancellationToken
        );
        return res.IsFailure
            ? []
            :
            [
                .. res.Value.Select(p => new SearchDocumentUpsertSpec(
                    ProviderKey,
                    ModuleName,
                    EntityTypeKey,
                    p.Id,
                    p.FullName,
                    Subtitle: p.Email ?? p.PrimaryPhoneFormatted,
                    Summary: $"{p.ConnectionStatus} · {p.RecordStatus}",
                    BodyText: $"{p.FirstName} {p.NickName} {p.LastName} {p.Email} {p.PrimaryPhoneFormatted}",
                    Tags: [p.ConnectionStatus.ToString(), p.RecordStatus.ToString()],
                    UrlPath: $"/crm/people/{p.Id:D}",
                    IsPublic: false,
                    ContentCollectionId: request.ContentCollectionId
                )),
            ];
    }
}
