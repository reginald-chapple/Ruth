using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Search.Infrastructure.Providers;

internal sealed class FinanceAccountsSearchDocumentProvider(IServiceProvider serviceProvider)
    : ISearchDocumentProvider
{
    public string ProviderKey => "finance.accounts";
    public string ModuleName => "Finance";
    public string EntityTypeKey => "Ruth.Finance.FinancialAccount";

    public async Task<IReadOnlyList<SearchDocumentUpsertSpec>> LoadDocumentsAsync(
        SearchProviderLoadRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var handler = serviceProvider.GetService<
            IQueryHandler<GetFinancialAccountsQuery, IReadOnlyList<FinancialAccountResponse>>
        >();
        if (handler is null)
        {
            return [];
        }

        var res = await handler.HandleAsync(
            new GetFinancialAccountsQuery(ActiveOnly: true),
            cancellationToken
        );
        return res.IsFailure
            ? []
            :
            [
                .. res
                    .Value.Take(Math.Clamp(request.MaxDocuments, 1, 500))
                    .Select(a => new SearchDocumentUpsertSpec(
                        ProviderKey,
                        ModuleName,
                        EntityTypeKey,
                        a.Id,
                        a.PublicName,
                        Subtitle: $"{a.Code} · {a.AccountKind}",
                        Summary: a.Description,
                        BodyText: $"{a.Name} {a.PublicName} {a.Code} {a.GlCode} {a.Description}",
                        Tags:
                        [
                            a.AccountKind.ToString(),
                            a.IsTaxDeductible ? "TaxDeductible" : "NonDeductible",
                        ],
                        UrlPath: "/finance/accounts",
                        IsPublic: a.IsPublic,
                        ContentCollectionId: request.ContentCollectionId
                    )),
            ];
    }
}
