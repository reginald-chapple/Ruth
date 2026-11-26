using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Invoices;

/// <summary>Query to list invoices filtered optionally by constituent, status, or branch.</summary>
/// <param name="PersonId">Optional filter by billed Person identifier.</param>
/// <param name="Status">Optional filter by invoice status.</param>
/// <param name="BranchId">Optional filter by Branch/Site identifier.</param>
public sealed record GetInvoicesQuery(
    Guid? PersonId = null,
    InvoiceStatus? Status = null,
    Guid? BranchId = null
) : IQuery<IReadOnlyList<InvoiceResponse>>;
