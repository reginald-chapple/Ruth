using Microsoft.AspNetCore.Components;
using Ruth.Modules.Finance.Contracts.Organizations;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Finance;

/// <summary>Detailed organization and corporate sponsor financial summary page.</summary>
public sealed partial class OrganizationDetail : ComponentBase
{
    private OrganizationDetailResponse? _detail;

    /// <summary>Gets or sets the route organization Person identifier.</summary>
    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        var result = await QueryDispatcher.QueryAsync<
            GetOrganizationByIdQuery,
            OrganizationDetailResponse
        >(new GetOrganizationByIdQuery(Id));
        _detail = result.IsSuccess ? result.Value : null;
    }
}
