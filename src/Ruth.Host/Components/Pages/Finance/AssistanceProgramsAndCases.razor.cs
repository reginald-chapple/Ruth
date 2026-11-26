using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Contracts.Assistance;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Finance;

/// <summary>Staff workspace for assistance, scholarship, fee-waiver, and micro-grant programs and case management.</summary>
public sealed partial class AssistanceProgramsAndCases : ComponentBase
{
    private static readonly AssistanceProgramCategory[] _categories =
        Enum.GetValues<AssistanceProgramCategory>();

    private List<FinancialAccountResponse> _accounts = [];
    private List<AssistanceProgramResponse> _programs = [];
    private List<AssistanceCaseResponse> _cases = [];

    private string _programName = "Community Scholarship & Relief";
    private string _programCode = "COMM-RELIEF";
    private AssistanceProgramCategory _category = AssistanceProgramCategory.Scholarship;
    private Guid _fundingAccountId;
    private string _budgetInput = "25000.00";
    private string _maxAwardInput = "2500.00";

    private Guid _selectedProgramId;
    private string _applicantIdInput = Guid.CreateVersion7().ToString("D");
    private string _requestedAmountInput = "500.00";
    private string _caseSummary = "Program tuition assistance request";

    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var accRes = await QueryDispatcher.QueryAsync<
            GetFinancialAccountsQuery,
            IReadOnlyList<FinancialAccountResponse>
        >(new GetFinancialAccountsQuery(ActiveOnly: true));
        if (accRes.IsSuccess)
        {
            _accounts = [.. accRes.Value];
            if (_fundingAccountId == Guid.Empty && _accounts.Count > 0)
            {
                _fundingAccountId = _accounts[0].Id;
            }
        }

        var progRes = await QueryDispatcher.QueryAsync<
            GetAssistanceProgramsQuery,
            IReadOnlyList<AssistanceProgramResponse>
        >(new GetAssistanceProgramsQuery());
        if (progRes.IsSuccess)
        {
            _programs = [.. progRes.Value];
            if (_selectedProgramId == Guid.Empty && _programs.Count > 0)
            {
                _selectedProgramId = _programs[0].Id;
            }
        }

        var caseRes = await QueryDispatcher.QueryAsync<
            GetAssistanceCasesQuery,
            IReadOnlyList<AssistanceCaseResponse>
        >(new GetAssistanceCasesQuery(Limit: 50));
        if (caseRes.IsSuccess)
        {
            _cases = [.. caseRes.Value];
        }
    }

    private async Task CreateProgramAsync()
    {
        if (
            !decimal.TryParse(
                _budgetInput,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var budget
            )
            || !decimal.TryParse(
                _maxAwardInput,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var maxAward
            )
        )
        {
            _isError = true;
            _statusMessage = "Enter valid numeric budget and max award amounts.";
            return;
        }

        var res = await CommandDispatcher.SendAsync<
            CreateAssistanceProgramCommand,
            AssistanceProgramResponse
        >(
            new CreateAssistanceProgramCommand(
                new AssistanceProgramSpec(
                    _programName,
                    _programCode,
                    _category,
                    _fundingAccountId,
                    budget,
                    maxAward
                )
            )
        );
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _selectedProgramId = res.Value.Id;
        _statusMessage = $"Created assistance program {res.Value.Name}.";
        await ReloadAsync();
    }

    private async Task SubmitCaseAsync()
    {
        if (
            !Guid.TryParse(_applicantIdInput, out var applicantId)
            || !decimal.TryParse(
                _requestedAmountInput,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var amount
            )
        )
        {
            _isError = true;
            _statusMessage = "Provide a valid applicant GUID and requested amount.";
            return;
        }

        var res = await CommandDispatcher.SendAsync<
            SubmitAssistanceCaseCommand,
            AssistanceCaseResponse
        >(new SubmitAssistanceCaseCommand(_selectedProgramId, applicantId, amount, _caseSummary));
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = $"Submitted case {res.Value.CaseNumber}.";
        await ReloadAsync();
    }

    private async Task ApproveFullAsync(AssistanceCaseResponse item)
    {
        var res = await CommandDispatcher.SendAsync<
            ReviewAssistanceCaseCommand,
            AssistanceCaseResponse
        >(
            new ReviewAssistanceCaseCommand(
                item.Id,
                AssistanceCaseStatus.Approved,
                item.RequestedAmount,
                DecisionNotes: "Approved via staff queue"
            )
        );
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = $"Approved case {res.Value.CaseNumber}.";
        await ReloadAsync();
    }

    private async Task DisburseRemainingAsync(AssistanceCaseResponse item)
    {
        var res = await CommandDispatcher.SendAsync<
            RecordAssistanceDisbursementCommand,
            AssistanceCaseResponse
        >(
            new RecordAssistanceDisbursementCommand(
                item.Id,
                AssistanceDisbursementMethod.DirectCheck,
                item.RemainingApprovedAmount,
                $"Applicant {item.ApplicantPersonId.ToString("D")[..8]}"
            )
        );
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = $"Disbursed remaining award on {res.Value.CaseNumber}.";
        await ReloadAsync();
    }
}
