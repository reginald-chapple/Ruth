using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Finance.Contracts.Statements;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Finance;

/// <summary>Staff workspace for generating print/PDF tax receipts, account statements, and annual batch runs.</summary>
public sealed partial class StatementsAndTaxReceipts : ComponentBase
{
    private static readonly FinancialStatementRecipientScope[] _scopes =
        Enum.GetValues<FinancialStatementRecipientScope>();

    private List<FinancialStatementTemplateResponse> _templates = [];
    private List<GeneratedFinancialStatementResponse> _statements = [];

    private Guid _selectedTemplateId;
    private FinancialStatementRecipientScope _selectedScope =
        FinancialStatementRecipientScope.Individual;
    private string _targetIdInput = Guid.CreateVersion7().ToString("D");
    private string _startDateInput = string.Create(
        CultureInfo.InvariantCulture,
        $"{DateTimeOffset.UtcNow.Year}-01-01"
    );
    private string _endDateInput = string.Create(
        CultureInfo.InvariantCulture,
        $"{DateTimeOffset.UtcNow.Year}-12-31"
    );
    private string? _previewHtml;
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
        var tmplRes = await QueryDispatcher.QueryAsync<
            GetFinancialStatementTemplatesQuery,
            IReadOnlyList<FinancialStatementTemplateResponse>
        >(new GetFinancialStatementTemplatesQuery(ActiveOnly: true));
        if (tmplRes.IsSuccess)
        {
            _templates = [.. tmplRes.Value];
            if (_selectedTemplateId == Guid.Empty && _templates.Count > 0)
            {
                _selectedTemplateId = _templates[0].Id;
            }
        }

        var stmtRes = await QueryDispatcher.QueryAsync<
            GetGeneratedFinancialStatementsQuery,
            IReadOnlyList<GeneratedFinancialStatementResponse>
        >(new GetGeneratedFinancialStatementsQuery(Limit: 50));
        if (stmtRes.IsSuccess)
        {
            _statements = [.. stmtRes.Value];
        }
    }

    private async Task GenerateSingleAsync()
    {
        if (
            !Guid.TryParse(_targetIdInput, out var targetGuid)
            || !TryParsePeriod(out var start, out var end)
        )
        {
            _isError = true;
            _statusMessage = "Provide a valid target GUID and YYYY-MM-DD period dates.";
            return;
        }

        var personId =
            _selectedScope == FinancialStatementRecipientScope.Household ? (Guid?)null : targetGuid;
        var householdId =
            _selectedScope == FinancialStatementRecipientScope.Household ? targetGuid : (Guid?)null;

        var res = await CommandDispatcher.SendAsync<
            GenerateFinancialStatementCommand,
            RenderedFinancialStatementDocumentResponse
        >(
            new GenerateFinancialStatementCommand(
                _selectedTemplateId,
                _selectedScope,
                personId,
                householdId,
                start,
                end
            )
        );
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _previewHtml = res.Value.HtmlContent;
        _statusMessage = $"Generated {res.Value.Statement.StatementNumber}.";
        await ReloadAsync();
    }

    private async Task RunBatchAsync()
    {
        if (!TryParsePeriod(out var start, out var end))
        {
            _isError = true;
            _statusMessage = "Provide valid YYYY-MM-DD period dates.";
            return;
        }

        var res = await CommandDispatcher.SendAsync<
            RunFinancialStatementBatchCommand,
            FinancialStatementBatchRunResponse
        >(new RunFinancialStatementBatchCommand(_selectedTemplateId, start, end));
        if (res.IsFailure)
        {
            _isError = true;
            _statusMessage = res.PrimaryError.Description;
            return;
        }

        _isError = false;
        _statusMessage = string.Create(
            CultureInfo.InvariantCulture,
            $"Batch run {res.Value.BatchRunId:D} generated {res.Value.GeneratedCount} statement(s)."
        );
        await ReloadAsync();
    }

    private bool TryParsePeriod(out DateOnly start, out DateOnly end)
    {
        var okStart = DateOnly.TryParseExact(
            _startDateInput,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out start
        );
        var okEnd = DateOnly.TryParseExact(
            _endDateInput,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out end
        );
        return okStart && okEnd;
    }
}
