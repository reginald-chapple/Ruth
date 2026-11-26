using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.PaymentMethods;

/// <summary>Query to list saved payment methods for a constituent or household.</summary>
/// <param name="PersonId">Optional filter by Person identifier.</param>
/// <param name="ActiveOnly">When true, returns only active payment methods.</param>
public sealed record GetSavedPaymentMethodsQuery(Guid? PersonId = null, bool ActiveOnly = true)
    : IQuery<IReadOnlyList<SavedPaymentMethodResponse>>;
