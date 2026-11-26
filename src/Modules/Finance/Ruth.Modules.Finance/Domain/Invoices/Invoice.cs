using System.Globalization;
using Ruth.Modules.Finance.Contracts.Invoices;
using Ruth.Modules.Finance.Domain.Accounts;
using Ruth.Modules.Finance.Domain.ValueObjects;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Finance.Domain.Invoices;

/// <summary>Invoice aggregate tracking line items, due dates, partial/full payment balance, and voiding invariants.</summary>
public sealed class Invoice : AggregateRoot<InvoiceId>, IAuditableEntity, IHasConcurrencyToken
{
    private readonly List<InvoiceLineItem> _lineItems = [];

    private Invoice() { }

    private Invoice(
        InvoiceId id,
        string invoiceNumber,
        Guid personId,
        Guid? householdId,
        Guid? branchId,
        DateOnly issueDate,
        DateOnly dueDate,
        string currencyCode,
        decimal subtotalAmount,
        string? memo
    )
        : base(id)
    {
        InvoiceNumber = invoiceNumber;
        PersonId = personId;
        HouseholdId = householdId;
        BranchId = branchId;
        IssueDate = issueDate;
        DueDate = dueDate;
        CurrencyCode = currencyCode;
        SubtotalAmount = subtotalAmount;
        PaidAmount = 0m;
        Status = InvoiceStatus.Draft;
        Memo = memo;
    }

    public string InvoiceNumber { get; private set; } = string.Empty;
    public Guid PersonId { get; private set; }
    public Guid? HouseholdId { get; private set; }
    public Guid? BranchId { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public DateOnly IssueDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public decimal SubtotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal BalanceDue => Math.Max(0m, SubtotalAmount - PaidAmount);
    public string? Memo { get; private set; }

    public IReadOnlyCollection<InvoiceLineItem> LineItems => _lineItems.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    /// <summary>Creates a validated <see cref="Invoice" /> aggregate with resolved account tax-deductibility snapshots.</summary>
    public static Result<Invoice> Create(
        Guid personId,
        DateOnly issueDate,
        DateOnly dueDate,
        string currencyCode,
        IReadOnlyList<(
            FinancialAccountId AccountId,
            string Description,
            decimal Quantity,
            decimal UnitAmount,
            bool IsTaxDeductible
        )> lines,
        bool issueImmediately = true,
        Guid? householdId = null,
        Guid? branchId = null,
        string? memo = null
    )
    {
        var headerResult = ValidateInvoiceHeader(
            personId,
            issueDate,
            dueDate,
            currencyCode,
            out var scale
        );
        if (headerResult.IsFailure)
        {
            return Result.Failure<Invoice>(headerResult.Errors);
        }

        if (lines is null || lines.Count == 0)
        {
            return FinanceErrors.InvoiceLineItemsRequired;
        }

        var id = InvoiceId.Create();
        var builtLines = BuildLineEntities(id, lines, scale, out var subtotal);
        if (builtLines is null || subtotal <= 0m)
        {
            return FinanceErrors.InvoiceLineItemsRequired;
        }

        var invoice = InstantiateInvoice(
            id,
            personId,
            householdId,
            branchId,
            issueDate,
            dueDate,
            headerResult.Value,
            subtotal,
            memo,
            issueImmediately
        );
        invoice._lineItems.AddRange(builtLines);
        return invoice;
    }

    private static Result<string> ValidateInvoiceHeader(
        Guid personId,
        DateOnly issueDate,
        DateOnly dueDate,
        string currencyCode,
        out int scale
    )
    {
        scale = 2;
        return personId == Guid.Empty ? FinanceErrors.PledgePersonRequired
            : dueDate < issueDate ? FinanceErrors.InvoiceDateRangeInvalid
            : !IsoCurrencyCatalog.TryGetScale(currencyCode, out var currency, out scale)
                ? FinanceErrors.CurrencyCodeInvalid
            : currency;
    }

    private static Invoice InstantiateInvoice(
        InvoiceId id,
        Guid personId,
        Guid? householdId,
        Guid? branchId,
        DateOnly issueDate,
        DateOnly dueDate,
        string currency,
        decimal subtotal,
        string? memo,
        bool issueImmediately
    )
    {
        var numberSuffix = id.Value.ToString("N")[..6].ToUpperInvariant();
        var invoiceNumber = string.Create(
            CultureInfo.InvariantCulture,
            $"INV-{issueDate:yyyyMMdd}-{numberSuffix}"
        );
        var invoice = new Invoice(
            id,
            invoiceNumber,
            personId,
            NormalizeGuid(householdId),
            NormalizeGuid(branchId),
            issueDate,
            dueDate,
            currency,
            subtotal,
            string.IsNullOrWhiteSpace(memo) ? null : memo.Trim()
        );
        if (issueImmediately)
        {
            _ = invoice.Issue();
        }

        return invoice;
    }

    /// <summary>Transitions a Draft invoice to Open.</summary>
    public Result Issue()
    {
        if (Status != InvoiceStatus.Draft)
        {
            return FinanceErrors.InvoicePaymentInvalid;
        }

        Status = InvoiceStatus.Open;
        return Result.Success();
    }

    /// <summary>Applies a positive payment toward this invoice, updating status to PartiallyPaid or Paid.</summary>
    public Result<bool> ApplyPayment(decimal amount)
    {
        var validMoney = Money.CreatePositive(amount, CurrencyCode);
        if (validMoney.IsFailure)
        {
            return Result.Failure<bool>(validMoney.Errors);
        }

        if (
            (Status != InvoiceStatus.Open && Status != InvoiceStatus.PartiallyPaid)
            || validMoney.Value.Amount > BalanceDue
        )
        {
            return FinanceErrors.InvoicePaymentInvalid;
        }

        PaidAmount += validMoney.Value.Amount;
        Status = PaidAmount >= SubtotalAmount ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;
        return Status == InvoiceStatus.Paid;
    }

    /// <summary>Reverses a settled payment amount when a transaction is refunded.</summary>
    public Result ReversePayment(decimal amount)
    {
        if (amount <= 0m || amount > PaidAmount)
        {
            return FinanceErrors.InvoicePaymentInvalid;
        }

        PaidAmount -= amount;
        Status = PaidAmount == 0m ? InvoiceStatus.Open : InvoiceStatus.PartiallyPaid;
        return Result.Success();
    }

    /// <summary>Voids an unpaid invoice.</summary>
    public Result Void(string? reason)
    {
        if (PaidAmount > 0m || Status == InvoiceStatus.Voided)
        {
            return FinanceErrors.InvoiceVoidNotAllowed;
        }

        Status = InvoiceStatus.Voided;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            var trimmed = reason.Trim();
            Memo = string.IsNullOrWhiteSpace(Memo) ? trimmed : $"{Memo} | Voided: {trimmed}";
        }

        return Result.Success();
    }

    /// <summary>Determines whether the invoice is overdue relative to <paramref name="today" />.</summary>
    public bool IsOverdue(DateOnly today)
    {
        return (Status == InvoiceStatus.Open || Status == InvoiceStatus.PartiallyPaid)
            && DueDate < today;
    }

    /// <summary>Repoints <see cref="PersonId" /> when duplicate person records are merged.</summary>
    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (PersonId != retiredPersonId || survivingPersonId == Guid.Empty)
        {
            return false;
        }

        PersonId = survivingPersonId;
        return true;
    }

    private static List<InvoiceLineItem>? BuildLineEntities(
        InvoiceId invoiceId,
        IReadOnlyList<(
            FinancialAccountId AccountId,
            string Description,
            decimal Quantity,
            decimal UnitAmount,
            bool IsTaxDeductible
        )> specs,
        int scale,
        out decimal subtotal
    )
    {
        subtotal = 0m;
        var list = new List<InvoiceLineItem>(specs.Count);
        foreach (var (accountId, description, quantity, unitAmount, isTaxDeductible) in specs)
        {
            if (
                accountId.IsEmpty
                || string.IsNullOrWhiteSpace(description)
                || quantity <= 0m
                || unitAmount < 0m
            )
            {
                return null;
            }

            var lineTotal = decimal.Round(
                quantity * unitAmount,
                scale,
                MidpointRounding.AwayFromZero
            );
            subtotal += lineTotal;
            list.Add(
                new InvoiceLineItem(
                    InvoiceLineItemId.Create(),
                    invoiceId,
                    accountId,
                    description.Trim(),
                    quantity,
                    decimal.Round(unitAmount, scale, MidpointRounding.AwayFromZero),
                    lineTotal,
                    isTaxDeductible
                )
            );
        }

        return list;
    }

    private static Guid? NormalizeGuid(Guid? id)
    {
        return id is { } guid && guid != Guid.Empty ? guid : null;
    }
}
