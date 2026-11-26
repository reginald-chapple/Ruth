using Microsoft.AspNetCore.Components;
using Ruth.Modules.Lms.Contracts.Portal;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Lms;

public sealed partial class LmsLearnerPortal : ComponentBase
{
    private IReadOnlyList<PublicCourseCatalogItemResponse> _catalog = [];

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var res = await QueryDispatcher.QueryAsync<
            GetPublicCourseCatalogQuery,
            IReadOnlyList<PublicCourseCatalogItemResponse>
        >(new GetPublicCourseCatalogQuery());
        if (res.IsSuccess)
        {
            _catalog = res.Value;
        }
    }
}
