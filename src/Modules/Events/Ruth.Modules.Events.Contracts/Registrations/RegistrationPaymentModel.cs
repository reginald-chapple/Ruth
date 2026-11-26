namespace Ruth.Modules.Events.Contracts.Registrations;

/// <summary>Payment settlement model configured on a RegistrationTemplate.</summary>
public enum RegistrationPaymentModel
{
    /// <summary>No payment required.</summary>
    Free = 0,

    /// <summary>Full balance required at submission.</summary>
    PayInFull = 1,

    /// <summary>Initial deposit at submission with subsequent installment payments.</summary>
    DepositAndInstallments = 2,

    /// <summary>Generates an open Finance Invoice for organizational or constituent billing.</summary>
    InvoiceBilling = 3,
}
