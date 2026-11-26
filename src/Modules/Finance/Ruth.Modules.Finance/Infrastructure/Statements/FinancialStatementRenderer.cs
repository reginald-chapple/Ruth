using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Finance.Domain.Statements;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Infrastructure.Statements;

internal sealed class FinancialStatementRenderer(
    IServiceProvider serviceProvider,
    IServiceScopeFactory scopeFactory,
    ILoggerFactory loggerFactory
) : IFinancialStatementRenderer
{
    public async Task<Result<(string HtmlContent, byte[] PdfBytes)>> RenderAsync(
        FinancialStatementTemplate template,
        GeneratedFinancialStatement statement,
        AggregatedStatementLedger ledger,
        CancellationToken cancellationToken
    )
    {
        var mergeContext = BuildMergeContext(template, statement, ledger);
        var renderedHeader = await EvaluateOptionalMergeAsync(
            template.HeaderMergeTemplate,
            mergeContext,
            cancellationToken
        );
        if (renderedHeader.IsFailure)
        {
            return Result.Failure<(string, byte[])>(renderedHeader.Errors);
        }

        var renderedFooter = await EvaluateOptionalMergeAsync(
            template.FooterMergeTemplate,
            mergeContext,
            cancellationToken
        );
        if (renderedFooter.IsFailure)
        {
            return Result.Failure<(string, byte[])>(renderedFooter.Errors);
        }

        var parameters = BuildComponentParameters(
            template,
            statement,
            ledger,
            renderedHeader.Value,
            renderedFooter.Value
        );
        var htmlResult = await RenderComponentHtmlAsync(parameters, cancellationToken);
        if (htmlResult.IsFailure)
        {
            return Result.Failure<(string, byte[])>(htmlResult.Errors);
        }

        var normalizedHtml = htmlResult.Value.TrimStart();
        var pdfBytes = SimplePdfDocumentWriter.WriteStatementPdf(template, statement, ledger);
        return (normalizedHtml, pdfBytes);
    }

    private async Task<Result<string?>> EvaluateOptionalMergeAsync(
        string? markup,
        IReadOnlyDictionary<string, object?> context,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(markup))
        {
            return Result.Success<string?>(value: null);
        }

        var mergeEngine = serviceProvider.GetService<IMergeTemplateEngine>();
        if (mergeEngine is null)
        {
            return Result.Success<string?>(System.Net.WebUtility.HtmlEncode(markup));
        }

        var rendered = await mergeEngine.RenderAsync(
            markup,
            context,
            new MergeRenderOptions(TemplateOutputMode.Html),
            cancellationToken
        );
        return rendered.IsFailure
            ? Result.Failure<string?>(rendered.Errors)
            : Result.Success<string?>(rendered.Value.RenderedContent);
    }

    private async Task<Result<string>> RenderComponentHtmlAsync(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken
    )
    {
        var razorRenderer = serviceProvider.GetService<IRazorTemplateRenderer>();
        if (razorRenderer is not null)
        {
            return await razorRenderer.RenderComponentAsync<FinancialStatementDocumentTemplate>(
                parameters,
                cancellationToken
            );
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        await using var htmlRenderer = new HtmlRenderer(scope.ServiceProvider, loggerFactory);
        var view = ParameterView.FromDictionary(
            new Dictionary<string, object?>(parameters, StringComparer.Ordinal)
        );
        var html = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = await htmlRenderer.RenderComponentAsync<FinancialStatementDocumentTemplate>(
                view
            );
            return root.ToHtmlString();
        });
        return Result.Success(html);
    }

    private static Dictionary<string, object?> BuildMergeContext(
        FinancialStatementTemplate template,
        GeneratedFinancialStatement statement,
        AggregatedStatementLedger ledger
    )
    {
        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["StatementNumber"] = statement.StatementNumber,
            ["RecipientName"] = statement.RecipientDisplayName,
            ["RecipientScope"] = statement.RecipientScope.ToString(),
            ["OrganizationName"] = template.OrganizationDisplayName,
            ["OrganizationTaxId"] = template.OrganizationTaxId ?? string.Empty,
            ["PeriodStart"] = statement.PeriodStartDate.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture
            ),
            ["PeriodEnd"] = statement.PeriodEndDate.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture
            ),
            ["CurrencyCode"] = statement.CurrencyCode,
            ["TaxDeductibleTotal"] = ledger.TaxDeductibleTotal.ToString(
                "F2",
                CultureInfo.InvariantCulture
            ),
            ["NonDeductiblePaidTotal"] = ledger.NonDeductiblePaidTotal.ToString(
                "F2",
                CultureInfo.InvariantCulture
            ),
            ["OpenInvoiceBalanceTotal"] = ledger.OpenInvoiceBalanceTotal.ToString(
                "F2",
                CultureInfo.InvariantCulture
            ),
        };
    }

    private static Dictionary<string, object?> BuildComponentParameters(
        FinancialStatementTemplate template,
        GeneratedFinancialStatement statement,
        AggregatedStatementLedger ledger,
        string? renderedHeaderHtml,
        string? renderedFooterHtml
    )
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [nameof(FinancialStatementDocumentTemplate.DocumentTitle)] =
                $"{template.Name} - {statement.StatementNumber}",
            [nameof(FinancialStatementDocumentTemplate.StatementNumber)] =
                statement.StatementNumber,
            [nameof(FinancialStatementDocumentTemplate.StatementKind)] = template.StatementKind,
            [nameof(FinancialStatementDocumentTemplate.RecipientScope)] = statement.RecipientScope,
            [nameof(FinancialStatementDocumentTemplate.RecipientDisplayName)] =
                statement.RecipientDisplayName,
            [nameof(FinancialStatementDocumentTemplate.OrganizationDisplayName)] =
                template.OrganizationDisplayName,
            [nameof(FinancialStatementDocumentTemplate.OrganizationTaxId)] =
                template.OrganizationTaxId,
            [nameof(FinancialStatementDocumentTemplate.LogoUrl)] = template.LogoUrl,
            [nameof(FinancialStatementDocumentTemplate.PeriodStartDate)] =
                statement.PeriodStartDate,
            [nameof(FinancialStatementDocumentTemplate.PeriodEndDate)] = statement.PeriodEndDate,
            [nameof(FinancialStatementDocumentTemplate.GeneratedAtUtc)] = statement.GeneratedAtUtc,
            [nameof(FinancialStatementDocumentTemplate.CurrencyCode)] = statement.CurrencyCode,
            [nameof(FinancialStatementDocumentTemplate.TaxDeductibleLines)] =
                ledger.TaxDeductibleLines,
            [nameof(FinancialStatementDocumentTemplate.NonDeductibleLines)] =
                ledger.NonDeductibleLines,
            [nameof(FinancialStatementDocumentTemplate.InvoiceSummaries)] = ledger.InvoiceSummaries,
            [nameof(FinancialStatementDocumentTemplate.PledgeSummaries)] = ledger.PledgeSummaries,
            [nameof(FinancialStatementDocumentTemplate.TaxDeductibleTotal)] =
                ledger.TaxDeductibleTotal,
            [nameof(FinancialStatementDocumentTemplate.NonDeductiblePaidTotal)] =
                ledger.NonDeductiblePaidTotal,
            [nameof(FinancialStatementDocumentTemplate.OpenInvoiceBalanceTotal)] =
                ledger.OpenInvoiceBalanceTotal,
            [nameof(FinancialStatementDocumentTemplate.RenderedHeaderHtml)] = renderedHeaderHtml,
            [nameof(FinancialStatementDocumentTemplate.RenderedFooterHtml)] = renderedFooterHtml,
            [nameof(FinancialStatementDocumentTemplate.CustomPrintCss)] = template.CustomPrintCss,
        };
    }
}
